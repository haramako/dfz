using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;

namespace Game.MapGenerator {

	[TestFixture]
	public class SimpleTest {

		RandomBase rand;


		[SetUp]
		public void SetUp(){
			rand = new RandomXS (System.DateTime.Now.Second);
		}

		[TearDown]
		public void TearDown(){
		}


		[TestCase]
		public void GenerateTest(){
			var gen = new Simple ();
			var map = new Map (64, 64);
			gen.Generate (map, rand);
			Console.WriteLine (map.Display ());
		}

	}
}
