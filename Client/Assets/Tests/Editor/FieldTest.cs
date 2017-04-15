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

		Character AddChara(int x, int y, string name = null, int hp = 100, int attack = 10, int defense = 10, CharacterType type = CharacterType.Enemy){
			var c = Character.CreateInstance ();
			c.Id = counter;
			if( name == null ) name = "E" + counter;
			c.Name = name;
			c.Hp = hp;
			c.MaxHp = hp;
			c.Attack = attack;
			c.Defense = defense;
			c.Type = type;
			counter++;
			field.Map.AddCharacter (c, new Point(x, y));
			if (type == CharacterType.Player) {
				field.SetPlayerCharacter (c);
			}
			return c;
		}

		public List<GameLog.ICommand> ack(){
			return field.Request (new GameLog.AckResponseRequest ());
		}

		public List<GameLog.ICommand> walk(Character c, Direction dir){
			var req = new GameLog.WalkRequest {
				CharacterId = c.Id,
				Path = new List<GameLog.Point> (){ c.Position + dir.ToPos () },
			};
			return field.Request (req);
		}

		public List<GameLog.ICommand> attack(Character c, Direction dir){
			var req = new GameLog.SkillRequest {
				CharacterId = c.Id,
				Dir = (int)dir,
				SkillId = G.FindSkillBySymbol("attack").Id,
			};
			return field.Request (req);
		}

		public void start(){
			field.StartThread ();
			ack ();
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
			field.NoUnity = false;
			field.RequestTimeoutMillis = 1000;
			field.Init(map);
		}

		[TearDown]
		public void TearDown(){
			System.Console.WriteLine ("teardown");
			field.Shutdown ();
		}

		[TestCase]
		public void TestShutdown(){
			AddChara(1, 1, "P1", type: CharacterType.Player);
			field.StartThread ();
			field.Request (new GameLog.AckResponseRequest ());
			System.Threading.Thread.Sleep (100);
			field.Shutdown ();
		}

		[TestCase]
		public void TestWalk(){
			var p = AddChara(1, 1, "P1", type: CharacterType.Player);
			start ();

			walk (p, Direction.North);

			Assert.AreEqual (new Point (1, 2), p.Position);
			Assert.AreEqual (Direction.North, p.Dir);
		}


		[TestCase]
		public void TestEnemyThinkToMove(){
			System.Console.WriteLine("hoge "+System.IO.Directory.GetCurrentDirectory());
			System.Environment.SetEnvironmentVariable("PATH", System.Environment.GetEnvironmentVariable("PATH") + ":/Users/makoto/dfz/Auto/dfz" );
			System.Console.WriteLine("fuga" + System.Environment.GetEnvironmentVariable("PATH"));

			var p = AddChara(1, 1, "P1", type: CharacterType.Player);
			var e1 = AddChara(1, 4, "E1");
			start ();

			walk (p, Direction.North);

			Assert.AreEqual (new Point (1, 3), e1.Position);
			Assert.AreEqual (Direction.South, e1.Dir);

			walk (p, Direction.South);

			Assert.AreEqual (new Point (1, 2), e1.Position);
			Assert.AreEqual (Direction.South, e1.Dir);

		}

		[TestCase]
		public void TestPlayerAttack(){
			var p = AddChara(1, 1, "P1", type: CharacterType.Player);
			var e1 = AddChara(1, 2, "E1");
			start ();

			attack (p, Direction.North);

			Assert.AreEqual (90, e1.Hp);

		}

	}
}
