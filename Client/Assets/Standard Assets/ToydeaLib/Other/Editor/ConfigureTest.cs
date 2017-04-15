using NUnit.Framework;
using System;

[TestFixture]
public class ConfigureTest {

	[TestCase]
	public void TestSetValue (){
		Configure.SetValue ("ConfTest.IntTest", "99");
		Assert.AreEqual (99, ConfTest.IntTest);

		Configure.SetValue ("ConfTest.Int64Test", "9999");
		Assert.AreEqual (9999, ConfTest.Int64Test);

		Configure.SetValue ("ConfTest.StringTest", "hoge");
		Assert.AreEqual ("hoge", ConfTest.StringTest);

		Configure.SetValue ("ConfTest.FloatTest", "99.9");
		Assert.IsTrue (Math.Abs (99.9 - ConfTest.FloatTest) < 0.01f);

		Configure.SetValue ("ConfTest.DoubleTest", "999.9");
		Assert.IsTrue (Math.Abs (999.9 - ConfTest.DoubleTest) < 0.01);

		Configure.SetValue ("ConfTest.BoolTest", "true");
		Assert.AreEqual (true, ConfTest.BoolTest);

		Configure.SetValue ("ConfTest.BoolTest", "false");
		Assert.AreEqual (false, ConfTest.BoolTest);

		Configure.SetValue ("ConfTest.BoolTest", "True");
		Assert.AreEqual (true, ConfTest.BoolTest);

	}
}
