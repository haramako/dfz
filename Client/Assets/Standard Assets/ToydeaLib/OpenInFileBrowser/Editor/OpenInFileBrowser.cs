using UnityEngine;
using System.Collections;

public static class OpenInFileBrowser {
	[UnityEditor.MenuItem("Window/Open In FileBrowser/persistentDataPath")]
	public static void OpenPersistentDataPath () {
		OpenInFileBrowser.Open (Application.persistentDataPath);
	}

	[UnityEditor.MenuItem("Window/Open In FileBrowser/temporaryCachePath")]
	public static void OpenTemporaryCachePath () {
		OpenInFileBrowser.Open (Application.temporaryCachePath);
	}

	[UnityEditor.MenuItem("Window/Open In FileBrowser/dataPath")]
	public static void OpenDataPath () {
		OpenInFileBrowser.Open (Application.dataPath);
	}


	/// <summary>
	/// ディレクトリ・フォルダを開く
	/// </summary>
	/// <param name="path">Path.</param>
	public static void Open (string path) {
		bool isInWinOS = UnityEngine.SystemInfo.operatingSystem.IndexOf ("Windows") != -1;
		if (isInWinOS) {
			OpenInWin (path);
			return;
		}

		bool isInMacOS = UnityEngine.SystemInfo.operatingSystem.IndexOf ("Mac OS") != -1;
		if (isInMacOS) {
			OpenInMac (path);
			return;
		}
	}

	/// <summary>
	/// [windows] explorerでフォルダを開く
	/// </summary>
	/// <param name="path">Path.</param>
	public static void OpenInWin (string path) {
		bool openInsidesOfFolder = false;
		string winPath = path.Replace ("/", "\\");
		if (System.IO.Directory.Exists (winPath)) {
			openInsidesOfFolder = true;
		}

		try {
			System.Diagnostics.Process.Start ("explorer.exe", (openInsidesOfFolder ? "/root," : "/select,") + winPath);
		} catch (System.ComponentModel.Win32Exception e) {
			e.HelpLink = "";
		}
	}

	/// <summary>
	/// [mac] finderでディレクトリを開く
	/// </summary>
	/// <param name="path">Path.</param>
	public static void OpenInMac (string path) {
		bool openInsidesOfFolder = false;
		string macPath = path.Replace ("\\", "/");
		if (System.IO.Directory.Exists (macPath)) {
			openInsidesOfFolder = true;
		}
		if (!macPath.StartsWith ("\"")) {
			macPath = "\"" + macPath;
		}
		if (!macPath.EndsWith ("\"")) {
			macPath = macPath + "\"";
		}

		string arguments = (openInsidesOfFolder ? "" : "-R ") + macPath;

		try {
			System.Diagnostics.Process.Start ("open", arguments);
		} catch (System.ComponentModel.Win32Exception e) {
			e.HelpLink = "";
		}
	}
}
