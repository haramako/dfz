using UnityEngine;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Game {
	public class TestUtil {
		public static int[][] ParseIntArray(string[] src){
			return src.Select (line => {
				return line.Trim ().Select (c => {
					int n;
					if (int.TryParse ("" + c, out n)) {
						return n;
					} else {
						return 0;
					}
				}).ToArray ();
			}).Reverse ().ToArray ();
		}

		public static Map CreateMap(params string[] src){
			var newMap = new Map (8,8);
			var data = TestUtil.ParseIntArray (src);
			int y = 0;
			foreach (var line in data) {
				int x = 0;
				foreach (var col in line) {
					newMap [x, y].Val = col;
					x++;
				}
				y++;
			}
			return newMap;
		}
	}

	[TestFixture]
	public class FieldTest {

		Map map;
		Field field;
		int counter = 1;

		Character AddChara(int x, int y, string name = null, int hp = 100, int attack = 10, int defense = 10){
			var c = Character.CreateInstance ();
			c.Id = counter;
			if( name == null ) name = "E" + counter;
			c.Name = name;
			c.Hp = hp;
			c.MaxHp = hp;
			c.Attack = attack;
			c.Defense = defense;
			counter++;
			field.Map.AddCharacter (c, new Point(x, y));
			return c;
		}

		public List<GameLog.ICommand> wait(){
			var result = new List<GameLog.ICommand> ();
			while (true) {
				if (field.State == GameState.Shutdowned) {
					throw new System.InvalidOperationException ("already shutdowned");
				}
				var cmd = field.SendQueue.Dequeue ();
				result.Add (cmd);
				System.Console.WriteLine (cmd);
				if (cmd is GameLog.WaitForRequest || cmd is GameLog.Shutdown) {
					return result;
				} else {
					field.RecvQueue.Enqueue (GameLog.AckRequest.CreateInstance ());
					return result;
				}
			}
		}

		public List<GameLog.ICommand> walk(Character c, Direction dir){
			var req = GameLog.WalkRequest.CreateInstance ();
			req.CharacterId = c.Id;
			req.Path = new List<GameLog.Point>(){ c.Position + dir.ToPos() };
			field.RecvQueue.Enqueue (req);
			return wait ();
		}

		public void start(){
			field.StartThread ();
			wait ();
		}

		[SetUp]
		public void SetUp(){
			map = TestUtil.CreateMap (
			//   0123456
				"0000000", // 5
				"0111110", // 4
				"0111110", // 3 
				"0101010", // 2
				"0111010", // 1
				"0000000");// 0

			field = new Field();
			field.Init(map);
		}

		[TearDown]
		public void TearDown(){
			field.Shutdown ();
		}

		[TestCase]
		public void TestShutdown(){
			AddChara(1, 1, "P1");
			field.StartThread ();
			field.Shutdown ();
		}

		[TestCase]
		public void TestWalk(){
			var p = AddChara(1, 1, "P1");
			start ();

			var cmd = (GameLog.Walk)walk (p, Direction.North).Last();
			Assert.AreEqual (p.Position, new Point (cmd.X, cmd.Y));
			Assert.AreEqual (p.Dir,	(Direction)cmd.Dir);

			Assert.AreEqual (new Point (1, 2), p.Position);
			Assert.AreEqual (Direction.North, p.Dir);
		}


		[TestCase]
		public void TestEnemy(){
			var p = AddChara(1, 4, "P1");
			var e1 = AddChara(1, 1, "E1");
			start ();

			var cmd = (GameLog.Walk)walk (p, Direction.North).Last();

		}

	}
}
