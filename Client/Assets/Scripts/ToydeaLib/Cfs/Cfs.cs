using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Ionic.Zlib;
using System.Security.Cryptography;
using RSG;

#if UNITY_EDITOR
using System.Net;
#endif

namespace Cfs {

	public enum CfsState {
		None = 0,
		DownloadingIndex,
		DownloadedIndex,
		DownloadingContents,
		Ready,
	}

	public class Cfs {
		public readonly string LocalRoot;
		public readonly Uri BaseUri;
		public readonly string BucketPath;
		public readonly string CryptoKey = "aiRue7ooouNee0IeooneeN2eel9Aifie";
		public readonly string CryptoIv = "Yee9zoogoow3Geiz";

		/// <summary>
		/// ファイル名によるフィルタ
		/// </summary>
		public Predicate<string> Filter;

		public CfsState state { get; private set; }
		public Bucket bucket { get; private set; }

		public bool Canceling { get; private set; }

		public Cfs(string localRoot, Uri baseUri, string bucketPath){
			LocalRoot = localRoot.TrimEnd(Path.DirectorySeparatorChar);
			BaseUri = baseUri;
			BucketPath = bucketPath;
			state = CfsState.None;
			prepare ();
		}

		void prepare(){
			// ディレクトリを作成する
			string[] pathParts = LocalRoot.Split(Path.DirectorySeparatorChar);
			for (int i = 0; i < pathParts.Length; i++) {
				if (i == 0 && pathParts [i] == "") pathParts [i] = "/";
				if (i > 0) pathParts [i] = Path.Combine (pathParts [i - 1], pathParts [i]);
				if (pathParts[i] != "" && !Directory.Exists (pathParts [i])) Directory.CreateDirectory (pathParts [i]);
			}
		}

		public void WriteBucket(string hash, byte[] data){
			writeFile (hash, new MemoryStream (data), ContentAttr.All);
			bucket = new Bucket (getStringFromHash (hash, ContentAttr.All), Filter);
		}

		public void WriteFile(string filename, byte[] data){
			var hash = bucket.Files [filename].Hash;
			writeFile (hash, new MemoryStream(data), ContentAttr.All);
		}

		public Uri UrlFromHash(string hash){
			return new Uri(BaseUri + "data/" + hash.Substring(0,2) + "/" + hash.Substring(2));
		}


#if UNITY_EDITOR
		
		public void DownloadIndexSync(){
			state = CfsState.DownloadingIndex;

			fetchSync (BucketPath, true);
			bucket = new Bucket(getStringFromHash (BucketPath, ContentAttr.All));

			state = CfsState.DownloadedIndex;
		}

		public void DownloadSync( IEnumerable<FileInfo> files){
			foreach( var file in files.Where (f => !File.Exists(LocalPathFromFile(f.Filename)))){
				//Debug.Log ("downloading " + file.Filename);
				fetchSync (file.Hash, true);
			}
		}

		void fetchSync(string hash, bool padded){
			var url = BaseUri + "data/" + hash.Substring(0,2) + "/" + hash.Substring(2);
			var request = (HttpWebRequest)WebRequest.Create (url);
			var res = request.GetResponse ();
			using (var stream = res.GetResponseStream ()) {
				writeFile (hash, stream, ContentAttr.All);
			}
		}

#endif
		
		//========================================================================
		// Hashを使ったアクセス関数
		//========================================================================

		string localPathFromHash(string hash){
			return Path.Combine (LocalRoot, hash);
		}

		Stream getStreamFromHash(string hash, ContentAttr attr){
			var stream = File.OpenRead (localPathFromHash (hash));
			return decode(stream, attr);
		}

		string getStringFromHash(string hash, ContentAttr attr){
			using (var stream = getStreamFromHash (hash, attr)) {
				return new StreamReader (stream).ReadToEnd ();
			}
		}

		public void writeFile(string hash, Stream stream, ContentAttr attr){
			var path = localPathFromHash (hash);

			// テンポラリファイルに書き込む
			var tmpPath = tempPath();
			using (var tmp = File.Open (tmpPath, FileMode.Create)) {
				var buf = new byte[8192];
				int len = 0;
				int read;
				while ((read = stream.Read (buf, 0, buf.Length)) > 0) {
					tmp.Write (buf, 0, read);
					len += read;
				}
				// パディングをする(AESがブロックサイズでしか復号できないため）
				if ((attr & ContentAttr.Crypted) != 0 && len % 16 != 0) {
					var pad = new byte[16];
					tmp.Write (pad, 0, 16 - len % 16);
				}
			}
			// アトミックにするために、Mone/Replaceを使用している
			if (File.Exists (path)) {
				File.Replace (tmpPath, path, null);
			} else {
				File.Move (tmpPath, path);
			}
		}

		Stream decode(Stream srcStream, ContentAttr attr){
			var result = srcStream;
			if ((attr & ContentAttr.Crypted) != 0) {
				var cipher = new AesManaged ();
				cipher.Padding = PaddingMode.None;
				cipher.Mode = CipherMode.CFB;
				cipher.KeySize = 256;
				cipher.BlockSize = 128;
				cipher.Key = System.Text.Encoding.UTF8.GetBytes (CryptoKey);
				cipher.IV = System.Text.Encoding.UTF8.GetBytes (CryptoIv);

				result = new CryptoStream (result, cipher.CreateDecryptor (), CryptoStreamMode.Read);
			}

			if( (attr & ContentAttr.Compressed) != 0 ){
				// ZLibのヘッダーを2byteを読み飛ばす
				// See: http://wiz.came.ac/blog/2009/09/zlibdll-zlibnet-deflatestream.html
				result.ReadByte (); 
				result.ReadByte ();
				result = new DeflateStream (result, CompressionMode.Decompress);
			}

			return result;
		}

		string tempPath(){
			return Path.Combine (LocalRoot, "cfstmpfile");
		}

		//========================================================================
		// ファイル名を使ったアクセス関数
		//========================================================================

		public Uri UrlFromFile(string filename){
			var hash = bucket.Files [filename].Hash;
			return new Uri(BaseUri + "data/" + hash.Substring(0,2) + "/" + hash.Substring(2));
		}

		public string LocalPathFromFile(string filename){
			FileInfo file;
			if (bucket.Files.TryGetValue (filename, out file)) {
				return Path.Combine (LocalRoot, file.Hash);
			} else {
				throw new FileNotFoundException (string.Format ("<color=red><b>cfs file '{0}' is not found.</b></color>", filename));
			}
		}

		public byte[] GetBytes(string filename){
			Configure.Log ("CfsLog", "open file " + filename);
			var file = bucket.Files [filename];
			var buf = new byte[file.OrigSize];
			using (var stream = decode (File.OpenRead (LocalPathFromFile (filename)), bucket.Files [filename].Attr)) {
				stream.Read (buf, 0, buf.Length);
				return buf;
			}
		}

		public Stream GetStream(string filename){
			Configure.Log ("CfsLog", "open file " + filename);
			return decode(File.OpenRead(LocalPathFromFile (filename)), bucket.Files[filename].Attr);
		}

		public string GetString(string filename){
			using (var stream = GetStream (filename)) {
				return new StreamReader (stream).ReadToEnd ();
			}
		}

		public bool ExistsInBucket(string filename){
			return bucket.Files.ContainsKey (filename);
		}

		public bool Exists(string filename){
            try {
                return File.Exists(LocalPathFromFile(filename));
            } catch {
                return false;
            }
		}

		/// <summary>
		/// キャッシュをすべてクリアする
		/// </summary>
		/// <returns>The cache.</returns>
		public void ClearCache(){
			try {
				Directory.Delete (LocalRoot, true);
				CancelDownload();
			}catch(Exception ex){
				Debug.LogException (ex);
			}
		}

		public void CancelDownload(){
			Canceling = true;
		}

		/// <summary>
		/// ファイルの状態をチェックする
		/// </summary>
		/// <param name="fullCheck">フルチェック（Hashの比較を行う）ならtrue</param>
		public CheckResult Check(bool fullCheck){
			var result = new CheckResult ();

			var filelist = Directory.GetFiles (LocalRoot).Select(f=>Path.GetFileName(f)).ToList();
			result.AllFileCount = filelist.Count;

			// バケットファイルを調査
			if (filelist.Contains (BucketPath)) {
				result.DownloadedFileCount++;
				filelist.Remove (BucketPath);
			}

			// バケット内のファイルを調査
			foreach (var file in bucket.Files.Values) {
				if (filelist.Contains (file.Hash)) {
					result.DownloadedFileCount++;
					filelist.Remove (file.Hash);

					// fullCheckならhashを比較する
					if (fullCheck) {
						var hash = getFileHash (localPathFromHash (file.Hash), file.Size);
						if (hash != file.Hash) {
							Debug.LogError ("invalid file hash '" + file.Filename + "' expect " + file.Hash + " but " + hash);
							result.ErrorCount++;
							try {
								File.Delete(localPathFromHash (file.Hash));
							}catch(Exception ex){
								Debug.LogError ("cannot delete error file " + file.Hash + ", " + ex);
							}
						}
					}
				}
			}

			// 不要になったファイルを削除する
			foreach (var f in filelist) {
				try {
					File.Delete(Path.Combine(LocalRoot, f));
				}catch(Exception ex){
					Debug.LogError ("cannot delete file " + f + ", " + ex);
					result.ErrorCount++;
				}
				result.RemovedFileCount++;
			}

			return result;
		}

		string getFileHash(string path, int size){
			using (var f = File.OpenRead (path)) {
				var md5 = MD5.Create ();
				var buf = new byte[size];
				f.Read (buf, 0, size); // padが入ってるかもしれないので、長さを制限する
				return BitConverter.ToString (md5.ComputeHash (buf)).ToLowerInvariant ().Replace ("-", "");
			}
		}
	}

	public class Bucket {
		public Dictionary<string,FileInfo> Files = new Dictionary<string,FileInfo>();

		/// <summary>
		/// 文字列からバケットを生成する
		/// </summary>
		/// <param name="src">バケットファイル</param>
		/// <param name="filter">
		///   ファイル名によるフィルタ（帰り値が真ならそのファイルを使用する）.
		///   nullを指定した場合はすべてのファイルを使用する
		/// </param>
		public Bucket(string src, Predicate<string> filter = null){
			foreach (var line in src.Split('\n')) {
				if (line == "") continue;
				var elements = line.Split ('\t');
				var hash = elements [0];
				var filename = elements [1];
				if (filter != null && !filter (filename)) continue;
				var size = Int32.Parse(elements [2]);
				var origHash = elements [4];
				var origSize = Int32.Parse(elements [5]);
				var attr = (ContentAttr)Int32.Parse(elements [6]);
				Files [filename] = new FileInfo (hash, filename, size, origHash, origSize, attr);
			}
		}
	}

	[Flags]
	public enum ContentAttr {
		None = 0,
		Compressed = 1,
		Crypted = 2,
		All = 3,
	}

	public class FileInfo {
		public readonly string Filename;
		public readonly int Size;
		public readonly string Hash;
		public readonly int OrigSize;
		public readonly string OrigHash;
		public readonly ContentAttr Attr;
		public FileInfo(string hash, string filename, int size, string origHash, int origSize, ContentAttr attr){
			Hash = hash;
			Filename = filename;
			Size = size;
			OrigHash = origHash;
			OrigSize = origSize;
			Attr = attr;
		}
	}

	public class DownloadProgress {
		public int AllCount;
		public int AllSize;
		public int LoadedCount;
		public int LoadedSize;

		public FileInfo CurrentFile;
		public int CurrentFileProgress;
	}

	public class CheckResult {
		public int AllFileCount;
		public int DownloadedFileCount;
		public int RemovedFileCount;
		public int ErrorCount;
	}

}