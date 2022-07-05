using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hotfix.BCBM
{
	public enum eBetID
	{
		BaoShiJie = 1,
		FaLaLi = 2,
		MaShaLaDi = 3,
		LanBoJiNi = 4,
		BenChi = 5,
		BaoMa = 6,
		LuHu = 7,
		JieBao = 8
	}

	public class ThisGameConfig
	{
		public List<int> betSet = new List<int>();
		public Dictionary<eBetID, int> ratio = new Dictionary<eBetID, int>();
		public Dictionary<int, eBetID> itemsPlaced;
		public float carRunnigTime = 12.0f;
		public int maxRecordCount = 20;
		public void Init()
		{
			betSet.Add(1000);
			betSet.Add(10000);
			betSet.Add(100000);
			betSet.Add(500000);
			betSet.Add(1000000);
			betSet.Add(5000000);
			
			ratio.Add(eBetID.BaoShiJie, 40);
			ratio.Add(eBetID.FaLaLi, 30);
			ratio.Add(eBetID.MaShaLaDi, 20);
			ratio.Add(eBetID.LanBoJiNi, 10);
			ratio.Add(eBetID.BenChi, 5);
			ratio.Add(eBetID.BaoMa, 5);
			ratio.Add(eBetID.LuHu, 5);
			ratio.Add(eBetID.JieBao, 5);

			itemsPlaced = new Dictionary<int, eBetID>() {
				{1, eBetID.BenChi },//1
				{2,eBetID.FaLaLi},//2
				{3,eBetID.BaoMa},//3
				{4,eBetID.MaShaLaDi},//4
				{5,eBetID.MaShaLaDi},//5
				{6,eBetID.BenChi},//6
				{7,eBetID.BaoShiJie},//7
				{8,eBetID.JieBao},//8
				{9,eBetID.BaoMa},//9
				{10,eBetID.LanBoJiNi},//10
				{11,eBetID.BenChi},//11
				{12,eBetID.BaoMa},//12
				{13,eBetID.LanBoJiNi},//13
				{14,eBetID.MaShaLaDi},//14
				{15,eBetID.FaLaLi},//15
				{16,eBetID.JieBao},//16
				{17,eBetID.LuHu},//17
				{18,eBetID.MaShaLaDi},//18
				{19,eBetID.LanBoJiNi},//19
				{20,eBetID.BaoMa},//20
				{21,eBetID.LuHu},//21
				{22,eBetID.FaLaLi},//22
				{23,eBetID.BaoShiJie},//23
				{24,eBetID.JieBao},//24
				{25,eBetID.LuHu},//25
				{26,eBetID.BaoShiJie},//26
				{27,eBetID.BenChi},//27
				{28,eBetID.JieBao},//28
				{29,eBetID.LanBoJiNi},//29
				{30,eBetID.FaLaLi},//30
				{31,eBetID.LuHu},//31
				{32,eBetID.BaoShiJie}//32
			};
		}
	}
}
