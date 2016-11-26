using UnityEngine;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Game {
	[TestFixture]
	public class MapTest {

		Map map;

		string pathToString(List<Point> path){
			if (path == null) {
				return "null";
			} else {
				return "[" + string.Join (",", path.Select (i => i.ToString ()).ToArray ()) + "]";
			}
		}

		[SetUp]
		public void SetUp(){
			map = TestUtil.CreateMap (
			//   01234
				"00000", // 4
				"01110", // 3
				"01110", // 2
				"01010", // 1
				"00000");// 0
		}

		[TestCase(1,1,3,1,4)]
		[TestCase(1,1,2,2,2)]
		[TestCase(1,1,3,3,3)]
		[TestCase(1,1,1,1,0)]
		public void TestSuccess (int x1, int y1, int x2, int y2, int len) {
			var path = map.PathFinder.FindPath (new Point (x1, y1), new Point (x2, y2), map.StepFlyable ());
			Assert.AreEqual (len, path.Count, "unexpected path " + pathToString(path));

			path = map.PathFinder.FindPath (new Point (x2, y2), new Point (x1, y1), map.StepFlyable ());
			Assert.AreEqual (len, path.Count, "unexpected reverse path " + pathToString(path));
		}

		public void TestAllowStartFromWall(){
			var path = map.PathFinder.FindPath (new Point (2, 1), new Point (2, 3), map.StepFlyable ());
			Assert.AreEqual (2, path.Count);
		}

		[TestCase(1,1,0,1)]
		[TestCase(1,1,3,4)]
		[TestCase(1,1,2,1)]
		public void TestNotFound (int x1, int y1, int x2, int y2) {
			var path = map.PathFinder.FindPath (new Point (x1, y1), new Point (x2, y2), map.StepFlyable ());
			Assert.IsNull(path, "must be null if not found");
		}

		[TestCase(1,1,3,1,0,0)]
		[TestCase(1,1,3,1,3,0)]
		[TestCase(1,1,3,1,4,4)]
		[TestCase(1,1,3,1,5,4)]
		public void TestLimited (int x1, int y1, int x2, int y2, int limit, int len) {
			var path = map.PathFinder.FindPath (new Point (x1, y1), new Point (x2, y2), map.StepFlyable (), len);
			if (len == 0) {
				Assert.IsNull (path, "must be null if not found");
			} else {
				Assert.AreEqual (len, path.Count, "unexpected path " + pathToString (path));
			}
		}

		[Test]
		public void TestLimitedWithZero (){
			var path = map.PathFinder.FindPath (new Point (1, 1), new Point (1, 1), map.StepFlyable (), 1);
			Assert.IsNotNull (path);
			Assert.AreEqual (0, path.Count);
		}
	}
}
