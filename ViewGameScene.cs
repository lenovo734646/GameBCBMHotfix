using AssemblyCommon;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Hotfix.Common;
using Hotfix.Model;
using LitJson;
using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

	public class RewardItem
	{
		public int pid;
		public eBetID betID;
	}

	public class BetItem
	{
		public BetItem(ViewGameScene v, int betID)
		{
			mainV_ = v;
			betID_ = betID;
			Init_();
		}

		public void SetMybet(long bet)
		{
			var txt = objBtn_.FindChildDeeply("selfScore").GetComponent<TextMeshProUGUI>();
			txt.text = bet.ToString();
		}

		public void SetTotalBet(long bet)
		{
			var txt = objBtn_.FindChildDeeply("totalScore").GetComponent<TextMeshProUGUI>();
			txt.text = bet.ToString();
		}

		void Init_()
		{
			//服务器BetID映射到UI名字上
			Dictionary<int, int> mapid = new Dictionary<int, int>();
			mapid.Add((int)eBetID.BaoMa, 6);
			mapid.Add((int)eBetID.BaoShiJie, 1);
			mapid.Add((int)eBetID.BenChi, 5);
			mapid.Add((int)eBetID.FaLaLi, 2);
			mapid.Add((int)eBetID.JieBao, 8);
			mapid.Add((int)eBetID.LanBoJiNi, 4);
			mapid.Add((int)eBetID.LuHu, 7);
			mapid.Add((int)eBetID.MaShaLaDi, 3);

			int objID = mapid[betID_];

			objBtn_ = mainV_.betStageRoot.FindChildDeeply($"{objID}");

			var btn = objBtn_.GetComponent<Button>();
			btn.onClick.AddListener(()=> {
				msg_set_bets_req msg = new msg_set_bets_req();
				msg.pid_ = mainV_.betSelected;
				msg.present_id_ = betID_;
				AppController.ins.network.SendMessage((short)GameMultiReqID.msg_set_bets_req, msg);
				
				if (lastBetTurn_ != mainV_.turn_)
					mainV_.lastBets.Clear();

				mainV_.lastBets.Add(msg);
				lastBetTurn_ = mainV_.turn_;
			});
		}
		int lastBetTurn_ = -1;
		int betID_;
		GameObject objBtn_;
		ViewGameScene mainV_;
	}

	public class ViewGameScene : ViewMultiplayerScene
	{
		public ViewGameScene(IShowDownloadProgress ip):base(ip)
		{
			var gm = (GameControllerMultiplayer)AppController.ins.currentApp.game;
			gm.mainView = this;
			itemsPlaced = new List<eBetID>() {
				eBetID.BenChi,
				eBetID.FaLaLi,
				eBetID.BaoMa,
				eBetID.MaShaLaDi,
				eBetID.MaShaLaDi,
				eBetID.BenChi,
				eBetID.BaoShiJie,
				eBetID.JieBao,
				eBetID.BaoMa,
				eBetID.LanBoJiNi,
				eBetID.BenChi,
				eBetID.BaoMa,
				eBetID.LanBoJiNi,
				eBetID.MaShaLaDi,
				eBetID.FaLaLi,
				eBetID.JieBao,
				eBetID.LuHu,
				eBetID.MaShaLaDi,
				eBetID.LanBoJiNi,
				eBetID.BaoMa,
				eBetID.LuHu,
				eBetID.FaLaLi,
				eBetID.BaoShiJie,
				eBetID.JieBao,
				eBetID.LuHu,
				eBetID.BaoShiJie,
				eBetID.BenChi,
				eBetID.JieBao,
				eBetID.LanBoJiNi,
				eBetID.FaLaLi,
				eBetID.LuHu,
				eBetID.BaoShiJie,
			};
		}

		protected override void SetLoader()
		{
			var ctrl = (GameController)AppController.ins.currentApp.game;
			LoadScene("Assets/Res/Games/BCBM/Scenes/MainScene.unity", null);

			for(int i = 1; i <= 8; i++) {
				int index = i;
				LoadAssets<Texture2D>($"Assets/Res/Games/BCBM/BCBM/UI/Record/record_{i}.png", t => rewardItemTexture.Add(index, t.Result));
			}
			
		}

		protected override IEnumerator OnResourceReady()
		{
			yield return base.OnResourceReady();

			canvas = GameObject.Find("Canvas");
			canvas3D = GameObject.Find("canvas3D");
			betStageRoot = canvas3D.FindChildDeeply("betAreaButtons");
			resultPanel = canvas.FindChildDeeply("ResultPanel");
			announcementFire = canvas.FindChildDeeply("AnnouncementFire");
			waitNextStateTip = canvas.FindChildDeeply("WaitNextStateTip");

			for (int i = 1; i <= 8; i++) { 
				BetItem bi = new BetItem(this, i);
				betItems_.Add(i, bi);
			}

			

			OnMyDataChanged(null, null);
		}
		IEnumerator ContinueBet()
		{
			for(int i = 0; i < lastBets.Count; i++) {
				AppController.ins.network.SendMessage((short)GameMultiReqID.msg_set_bets_req, lastBets[i]);
				yield return new WaitForSeconds(0.1f);
			}
		}

		void OnBetClick(bool showBet)
		{
			var tog_OpenBet = canvas.FindChildDeeply("ChouMaList").GetComponent<Toggle>();

			if (showBet) {
				betStageRoot.StartDoTweenAnim(false);
			}
			else {
				betStageRoot.StartDoTweenAnim(true);
			}
			tog_OpenBet.isOn = showBet;
		}

		void OnMyDataChanged(object sender, EventArgs evt)
		{
			var useInfo = canvas.FindChildDeeply("UserInfo");
			var head = useInfo.FindChildDeeply("Head").GetComponent<Image>();
			AppController.ins.self.gamePlayer.SetHeadPic(head);

			var frame = useInfo.FindChildDeeply("HeadFrame").GetComponent<Image>();
			AppController.ins.self.gamePlayer.SetHeadFrame(frame);

			var nickName = useInfo.FindChildDeeply("UserName").GetComponent<TextMeshProUGUI>();
			nickName.text = AppController.ins.self.gamePlayer.nickName;

			var goldText = useInfo.FindChildDeeply("UserMoney").GetComponent<TextMeshProUGUI>();
			goldText.text = Utils.FormatGoldShow(AppController.ins.self.gamePlayer.items[(int)ITEMID.GOLD]);
		}

		public override void Close()
		{
			betItems_.Clear();

			AppController.ins.self.gamePlayer.onDataChanged -= OnMyDataChanged;
			base.Close();
		}

		public override void OnNetMsg(int cmd, string json)
		{
			switch (cmd) {
				case (int)GameMultiRspID.msg_send_color: {
					
				}
				break;
			}
		}

		IEnumerator CountDown_(float t, TextMeshProUGUI txtCounter)
		{
			float tLeft = t;
			while (tLeft > 0) {
				tLeft -= 1.0f;
				yield return new WaitForSeconds(0.95f);
				if (tLeft < 0.0f) tLeft = 0.0f;
				txtCounter.text = tLeft.ToString();
			}
			yield return 0;
		}

		public IEnumerator ShowBetState_(bool delay)
		{
			if (delay)
				yield return new WaitForSeconds(2.0f);

			foreach (var bi in betItems_) {
				bi.Value.SetMybet(0);
				bi.Value.SetTotalBet(0);
			}
		}

		public override void OnStateChange(msg_state_change msg)
		{
			GameControllerBase.GameState st = (GameControllerBase.GameState)int.Parse(msg.change_to_);
			var txtCounter = canvas.FindChildDeeply("GameTimeCounter").FindChildDeeply("Time").GetComponent<TextMeshProUGUI>();
			var gameStateText = canvas.FindChildDeeply("gameStateText").GetComponent<TextMeshProUGUI>();

			var StartBet =  canvas.FindChildDeeply("StartBet");
			var StopBet = canvas.FindChildDeeply("StopBet");

			StartBet.SetActive(false);
			StopBet.SetActive(false);
		
			if (st == GameControllerBase.GameState.state_wait_start) {
				myTotalBet_ = 0;
				resultPanel.SetActive(false);
				gameStateText.text = LangMultiplayer.WaitingForBet;
				waitNextStateTip.SetActive(false);
				StartBet.SetActive(true);
				this.StartCor(ShowBetState_(int.Parse(msg.time_left) > 3), false);
			}
			else if (st == GameControllerBase.GameState.state_do_random) {
				gameStateText.text = LangMultiplayer.RandomResult;
				resultPanel.SetActive(false);
				StopBet.SetActive(true);
			}
			else if (st == GameControllerBase.GameState.state_rest_end) {
				gameStateText.text = LangMultiplayer.BalanceResult;
			}

			if (int.Parse(msg.time_total_) > 0)
				stateTimePercent = int.Parse(msg.time_left) * 1.0f / int.Parse(msg.time_total_);

			this.StartCor(CountDown_(int.Parse(msg.time_left), txtCounter), false);
		}

		public override void OnPlayerSetBet(msg_player_setbet msg)
		{
			int it = (int)itemsPlaced[int.Parse(msg.present_id_)];
			var bi = betItems_[it];
			
			bi.SetTotalBet(long.Parse(msg.max_setted_));
		}

		public override void OnMyBet(msg_my_setbet msg)
		{
			int it = (int)itemsPlaced[int.Parse(msg.present_id_)];
			var bi = betItems_[it];
			bi.SetMybet(long.Parse(msg.my_total_set_));

			myTotalBet_ += long.Parse(msg.set_);
		}

		public override GamePlayer OnPlayerEnter(msg_player_seat msg)
		{
			var pp = base.OnPlayerEnter(msg);
			return pp;
		}

		public override void OnPlayerLeave(msg_player_leave msg)
		{
			var game = AppController.ins.currentApp.game;
			var pp = game.GetPlayer(msg.pos_);
			game.RemovePlayer(int.Parse(msg.pos_));
		}

		IEnumerator DoRandomResult_(msg_random_result_base msg)
		{
			var pmsg = (msg_random_result_slwh)msg;
			yield return 0;
		}

		public override void OnRandomResult(msg_random_result_base msg)
		{
			this.StartCor(DoRandomResult_(msg), true);
		}

		GameObject CreateGameRecordItem_(int pid)
		{
			var item = itemsPlaced[pid];


			return null;
		}

		public override void OnLastRandomResult(msg_last_random_base msg)
		{
			var pmsg = (msg_last_random_slwh)msg;
			MyDebug.LogFormat("OnLastRandomResult:animals{0},  color_:{1}", pmsg.ani_, pmsg.color_);
			List<int> pids = Globals.Split(pmsg.pids_, ",");
			List<int> turns = Globals.Split(pmsg.turn_, ",");

			if (pids.Count == 0 || turns.Count == 0) return;

			//服务器是最新的在新前面,需要倒过来显示
			for (int i = pids.Count - 1; i >= 0; i--) {
				var rec = CreateGameRecordItem_(pids[i]);
				recordViewport.AddChild(rec);
			}
			lastTurn_ = turns.First();
			var gameCountText = canvas.FindChildDeeply("gameCountText").GetComponent<TextMeshProUGUI>();
			gameCountText.text = lastTurn_.ToString();
		}

		public override void OnBankDepositChanged(msg_banker_deposit_change msg)
		{
			if (banker == null) return;
			var BankerInfo = canvas.FindChildDeeply("BankerInfo");
			var BankerMoney = BankerInfo.FindChildDeeply("BankerName").GetComponent<TextMeshProUGUI>();
			BankerMoney.text = msg.credits_;
		}

		//庄家信息
		public override void OnBankPromote(msg_banker_promote msg)
		{
			var pp = AppController.ins.currentApp.game.GetPlayer(msg.uid_);

			var BankerInfo = canvas.FindChildDeeply("BankerInfo");
			var BankerProfile = BankerInfo.FindChildDeeply("BankerInfo").GetComponent<Image>();
			pp.SetHeadPic(BankerProfile);
			var BankerProfileFrame = BankerInfo.FindChildDeeply("BankerProfileFrame").GetComponent<Image>();
			pp.SetHeadFrame(BankerProfileFrame);
		
			var BankerName = BankerInfo.FindChildDeeply("BankerName").GetComponent<TextMeshProUGUI>();
			if (int.Parse(msg.is_sys_banker_) == 0) {
				BankerName.text = Language.gameName;
			}
			else {
				BankerName.text = pp.nickName;
			}

			var BankerMoney = BankerInfo.FindChildDeeply("BankerName").GetComponent<TextMeshProUGUI>();
			BankerMoney.text = msg.deposit_;
		}

		public override void OnGameInfo(msg_game_info msg)
		{
			var gameCountText = canvas.FindChildDeeply("gameCountText").GetComponent<TextMeshProUGUI>();
			gameCountText.text = msg.turn_;
			turn_ = int.Parse(msg.turn_);

			List<int> pids = Globals.Split(msg.pids_, ",");
			List<int> counts = Globals.Split(msg.counts_, ",");
			int sanYuanC = 0, siXiC = 0, caiJingC = 0, songDengC = 0, sanDianC = 0;
			for(int i = 0; i < pids.Count; i++) {
				
			}

			var txts = canvas.FindChildDeeply("Texts");
			var sixiText = txts.FindChildDeeply("sixiText").GetComponent<TextMeshProUGUI>();
			sixiText.text = Language.DaSiXi + "X" + siXiC;

			var sanyuanText = txts.FindChildDeeply("sanyuanText").GetComponent<TextMeshProUGUI>();
			sanyuanText.text = Language.DaSanYuan + "X" + sanYuanC;

			var songDengText = txts.FindChildDeeply("songDengText").GetComponent<TextMeshProUGUI>();
			songDengText.text = Language.SondDeng + "X" + songDengC;

			var caiJinText = txts.FindChildDeeply("caiJinText").GetComponent<TextMeshProUGUI>();
			caiJinText.text = Language.CaiJing + "X" + caiJingC;

			var sanDianText = txts.FindChildDeeply("sanDianText").GetComponent<TextMeshProUGUI>();
			sanDianText.text = Language.SanDian + "X" + sanDianC;

			var allText = txts.FindChildDeeply("allText").GetComponent<TextMeshProUGUI>();
			allText.text = Language.TotalTurn + "X" + turn_;
		}

		public override void OnGameReport(msg_game_report msg)
		{
			resultPanel.SetActive(true);

			var ResuletScrollView = resultPanel.FindChildDeeply("ResuletScrollView");
			var cont = ResuletScrollView.FindChildDeeply("Content");
			
			cont.RemoveAllChildren();
			
						
			var spine_stage = resultPanel.FindChildDeeply("spine_stage");
			var sk = spine_stage.GetComponent<SkeletonGraphic>();
			sk.AnimationState.SetAnimation(0, "animation", true);

			var betText = resultPanel.FindChildDeeply("betText").GetComponent<Text>();
			betText.text = msg.pay_;
			var winText = resultPanel.FindChildDeeply("winText").GetComponent<Text>();
			winText.text = msg.actual_win_;
			var winEnjoyGame = canvas.FindChildDeeply("winEnjoyGame");
			var winColorBG_1 = winEnjoyGame.FindChildDeeply("winColorBG_1");
			var winColorBG_2 = winEnjoyGame.FindChildDeeply("winColorBG_2");
			var winColorBG_3 = winEnjoyGame.FindChildDeeply("winColorBG_3");
			winColorBG_1.SetActive(false); 
			winColorBG_2.SetActive(false); 
			winColorBG_3.SetActive(false);

			var winAnimal = resultPanel.FindChildDeeply("winAnimal");
			var winSanYuan = resultPanel.FindChildDeeply("winSanYuan");
			var winSiXi = resultPanel.FindChildDeeply("winSiXi");
			var winShandian = resultPanel.FindChildDeeply("winShandian");
			var winCaiJin = resultPanel.FindChildDeeply("winCaiJin");
			var winSongDeng = resultPanel.FindChildDeeply("winSongDeng");
			winAnimal.SetActive(false);
			winSanYuan.SetActive(false);
			winSiXi.SetActive(false);
			winShandian.SetActive(false);
			winCaiJin.SetActive(false);
			winSongDeng.SetActive(false);
		}

		public override void OnGoldChange(msg_deposit_change2 msg)
		{
			int pos = AppController.ins.self.gamePlayer.serverPos;
			if (int.Parse(msg.pos_) == pos) {
				if(int.Parse(msg.display_type_) == (int)msg_deposit_change2.dp.display_type_sync_gold) {
					AppController.ins.self.gamePlayer.items.SetKeyVal((int)ITEMID.GOLD, long.Parse(msg.credits_));
					AppController.ins.self.gamePlayer.DispatchDataChanged();
				}
			}
		}

		public override void OnGoldChange(msg_currency_change msg)
		{
			if(msg.why_ == "0") {
				AppController.ins.self.gamePlayer.items.SetKeyVal((int)ITEMID.GOLD, long.Parse(msg.credits_));
				AppController.ins.self.gamePlayer.DispatchDataChanged();
			}

		}
		long myTotalBet_
		{
			get {
				return myTotalBet__;
			}
			set {
				myTotalBet__ = value;
				var betText = canvas.FindChildDeeply("BottomBG").FindChildDeeply("betText").GetComponent<TextMeshProUGUI>();
				betText.text = Utils.FormatGoldShow(myTotalBet__);
			}
		}

		public AddressablesLoader.LoadTask<Material>  matRed, matGreen, matYellow;
		public GameObject betStageRoot, animalRot, arrowRot, jumpTarget, 
			canvas, resultPanel, huaBan, bigSmallViewport, recordViewport, canvas3D,
			announcementFire, waitNextStateTip;
		
		public int betSelected = 1, turn_;
		public List<msg_set_bets_req> lastBets = new List<msg_set_bets_req>();
		Dictionary<int, BetItem> betItems_ = new Dictionary<int, BetItem>();

		int lastPointerPos = 0, lastBigSmall = 0, lastTurn_ = 0;
		List<int> lstColor, lstRates, animalIDs;
		Dictionary<int,Texture2D> rewardItemTexture = new Dictionary<int, Texture2D>();
		eBetID pidMain, pidSub;
		float stateTimePercent = 0.0f;
		long myTotalBet__ = 0;
		List<eBetID> itemsPlaced;
		GamePlayer banker;
	}
}
