using AssemblyCommon;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Hotfix.Common;
using Hotfix.Common.MultiPlayer;
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
			objBtn_ = mainV_.betStageRoot.FindChildDeeply($"{betID_}");
			var btn = objBtn_.GetComponent<Button>();
			btn.onClick.AddListener(()=> {
				MyDebug.LogFormat("Set Bet:{0}", betID_);
				msg_set_bets_req msg = new msg_set_bets_req();
				msg.pid_ = mainV_.betSelected;
				msg.present_id_ = betID_;
				App.ins.network.SendMessage((ushort)GameMultiReqID.msg_set_bets_req, msg);
				
				if (mainV_.lastBetTurn_ != mainV_.turn_)
					mainV_.lastBets.Clear();

				mainV_.lastBets.Add(msg);
				mainV_.lastBetTurn_ = mainV_.turn_;
			});
		}
		int betID_;
		GameObject objBtn_;
		ViewGameScene mainV_;
	}

	class RunningCar
	{
		public RunningCar(GameObject obj)
		{
			carModel = obj;
			Init_();
		}

		void Init_()
		{
			breaker_ = carModel.FindChildDeeply("刹车灯");
			lightNear_ = carModel.FindChildDeeply("近光灯");
			lightFar_ = carModel.FindChildDeeply("远光灯");
			stopEffect_ = carModel.FindChildDeeply("paoche_停下尾气");
			startEffect_ = carModel.FindChildDeeply("paoche_开动尾气");
			Stoped();
		}

		public IEnumerator StartRace(float timePercent)
		{
			breaker_.SetActive(false);
			//近光灯闪一下
			for(int i = 0; i < 3; i++) {
				lightNear_.SetActive(false);
				yield return new WaitForSeconds(0.2f * timePercent);
				lightNear_.SetActive(true);
				yield return new WaitForSeconds(0.2f * timePercent);
			}
			lightNear_.SetActive(false);
			//切换远光灯
			lightFar_.SetActive(true);
			//开始启动特效
			startEffect_.SetActive(true);
			startEffect_.StartParticles();
		}
		//全速跑
		public void Running()
		{
			startEffect_.StopParticles();
			startEffect_.SetActive(false);
		}

		//快到终点,开始减速
		public void AboutToEnd()
		{
			breaker_.SetActive(true);
			lightNear_.SetActive(true);
		}

		public void Stoping()
		{
			stopEffect_.SetActive(true);
			stopEffect_.StartParticles();
		}
		public void Stoped()
		{
			breaker_.SetActive(true);
			lightNear_.SetActive(true);
			lightFar_.SetActive(false);
			stopEffect_.SetActive(false);
			startEffect_.SetActive(false);
		}

		public GameObject carModel;
		GameObject breaker_, lightNear_, lightFar_, stopEffect_, startEffect_;
	
	}

	public class ViewGameScene : ViewMultiplayerScene
	{
		public ViewGameScene(IShowDownloadProgress ip):base(ip)
		{
			var gm = (GameControllerMultiplayer)App.ins.currentApp.game;
			gm.mainView = this;
		}

		protected override void SetLoader()
		{
			var ctrl = (GameController)App.ins.currentApp.game;
			LoadScene("Assets/Res/Games/BCBM/Scenes/MainScene.unity", null);

			for(int i = 1; i <= 8; i++) {
				int index = i;
				LoadAssets<Texture2D>($"Assets/Res/Games/BCBM/BCBM/UI/Record/record_{i}.png", t => rewardItemTexture.Add(index, t.Result));
			}

			for (int i = 1; i <= 8; i++) {
				int index = i;
				LoadAssets<Texture2D>($"Assets/Res/Games/BCBM/BCBM/UI/rule/{i}.png", t => resultItemTexture.Add(index, t.Result));
			}

			LoadAssets<Font>("Assets/Res/Games/BCBM/BCBM/UI/Fonts/jeisuan.fontsettings", t => fontWin = t.Result);
			LoadAssets<Font>("Assets/Res/Games/BCBM/BCBM/UI/Fonts/jiehsuan-.fontsettings", t => fontLose = t.Result);
		}

		protected override IEnumerator OnResourceReady()
		{
			var view = GameObject.Find("View");
			canvas = GameObject.Find("Canvas");
			canvas3D = GameObject.Find("Canvas3D");
			betStageRoot = canvas3D.FindChildDeeply("betAreaButtons");
			resultPanel = canvas.FindChildDeeply("ResultPanel");
			announcementFire = canvas.FindChildDeeply("AnnouncementFire");
			waitNextStateTip = canvas.FindChildDeeply("WaitNextStateTip");
			recordViewport = canvas3D.FindChildDeeply("OSAScrollView").FindChildDeeply("Content");

			carWinFx = view.FindChildDeeply("carWinFx");

			var betSelectBtns = canvas.FindChildDeeply("BetSelectBtns");
			var SelectEffect = betSelectBtns.FindChildDeeply("SelectEffect");

			car = new RunningCar(GameObject.Find("Paoche"));

			for (int i = 0; i <= 5; i++) {
				int bi = i;
				var beti = betSelectBtns.FindChildDeeply(i.ToString());
				var checki = beti.GetComponent<Toggle>();
				checki.onValueChanged.AddListener((seleced) => {
					if (!seleced) return;
					SelectEffect.transform.position = beti.transform.position;
					betSelected = bi + 1; //加压注ID要+1
				});
			}

			for (int i = 1; i <= 8; i++) { 
				BetItem bi = new BetItem(this, i);
				betItems_.Add(i, bi);
			}

			{
				var ChouMaList = view.FindChildDeeply("ChouMaList");
				var obj = ChouMaList.FindChildDeeply("bcbm_couma_1K");
				chipModels.Add(obj);
				obj = ChouMaList.FindChildDeeply("bcbm_couma_1W");
				chipModels.Add(obj);
				obj = ChouMaList.FindChildDeeply("bcbm_couma_10W");
				chipModels.Add(obj);
				obj = ChouMaList.FindChildDeeply("bcbm_couma_50W");
				chipModels.Add(obj);
				obj = ChouMaList.FindChildDeeply("bcbm_couma_100W");
				chipModels.Add(obj);
				obj = ChouMaList.FindChildDeeply("bcbm_couma_500W");
				chipModels.Add(obj);
			}
			
			var btn_BetAgain = canvas.FindChildDeeply("btn_BetAgain").GetComponent<Button>();

			btn_BetAgain.onClick.AddListener(() => {
				if(!isAutoBeting)
					this.StartCor(ContinueBet(), true);
			});

			var btnClean = canvas.FindChildDeeply("btnClean");
			btnClean.OnClick(() => {
				msg_clear_my_bets msg = new msg_clear_my_bets();
				App.ins.network.SendMessage((ushort)GameMultiReqID.msg_clear_my_bets, msg);
				myTotalBet_ = 0;
			});
	
			var btnOK = resultPanel.FindChildDeeply("btnOK");
			btnOK.OnClick(() => {
				resultPanel.SetActive(false);
			});

			btnToBanker = canvas.FindChildDeeply("BankerInfo").FindChildDeeply("btnToBanker");
			btnToBanker.OnClick(()=>{
				if (!isApplyingBanker) {
					msg_apply_banker_req msg = new msg_apply_banker_req();
					App.ins.network.SendMessage((ushort)GameMultiReqID.msg_apply_banker_req, msg);
				}
				else {
					msg_cancel_banker_req msg = new msg_cancel_banker_req();
					App.ins.network.SendMessage((ushort)GameMultiReqID.msg_cancel_banker_req, msg);
				}
			});

			var txt = btnToBanker.FindChildDeeply("Text (TMP)").GetComponent<TextMeshProUGUI>();
			txt.text = Language.ApplyBanker;

			var WaitingBankerName = canvas.FindChildDeeply("WaitingBankerName").GetComponent<TextMeshProUGUI>();
			WaitingBankerName.text = string.Format(Language.ApplyingBanker, applyList_.Count);

			var Toggle_Menu = canvas.FindChildDeeply("Toggle_Menu").GetComponent<Toggle>();
			Toggle_Menu.onValueChanged.AddListener((seleced) => {
				if (seleced)
					Toggle_Menu.gameObject.FindChildDeeply("Option").StartDoTweenAnim(false);
				else
					Toggle_Menu.gameObject.FindChildDeeply("Option").StartDoTweenAnim(true);
			});

			var btn_bank = Toggle_Menu.gameObject.FindChildDeeply("btn_Bank");
			btn_bank.OnClick(() => {
				ViewBankLogin bank = new ViewBankLogin(null);
				App.ins.currentApp.game.OpenView(bank);
			});

			var btn_rule = Toggle_Menu.gameObject.FindChildDeeply("btn_Rule");
			var RulePanel = canvas.FindChildDeeply("RulePanel");
			btn_rule.OnClick(() => {
				RulePanel.StartDoTweenAnim(true);
			});


			var btn_set = Toggle_Menu.gameObject.FindChildDeeply("btn_Set");
			var btn_exit = Toggle_Menu.gameObject.FindChildDeeply("btn_Exit");
			btn_exit.OnClick(() => {
				ViewPopup pop = ViewPopup.Create(LangUITip.ConfirmLeave, ViewPopup.Flag.BTN_OK_CANCEL, () => {
					App.ins.StartCor(App.ins.CheckUpdateAndRun(App.ins.conf.defaultGame, null, false), false);
				});
			});


			//百人类游戏直接进游戏房间
			var handle1 = App.ins.network.EnterGameRoom(1, 0);
			yield return handle1;
			if ((int)handle1.Current == 0) {
				ViewToast.Create(LangNetWork.EnterRoomFailed);
			}

			App.ins.self.gamePlayer.onDataChanged += OnMyDataChanged;
			OnMyDataChanged(null, null);
		}
	
		IEnumerator ContinueBet()
		{
			isAutoBeting = true;
			for (int i = 0; i < lastBets.Count; i++) {
				App.ins.network.SendMessage((ushort)GameMultiReqID.msg_set_bets_req, lastBets[i]);
				yield return new WaitForSeconds(0.1f);
			}
			isAutoBeting = false;
		}

		void OnMyDataChanged(object sender, EventArgs evt)
		{
			var useInfo = canvas.FindChildDeeply("UserInfo");
			var head = useInfo.FindChildDeeply("Head").GetComponent<Image>();
			App.ins.self.gamePlayer.SetHeadPic(head);

			var frame = useInfo.FindChildDeeply("HeadFrame").GetComponent<Image>();
			App.ins.self.gamePlayer.SetHeadFrame(frame);

			var nickName = useInfo.FindChildDeeply("UserName").GetComponent<TextMeshProUGUI>();
			nickName.text = App.ins.self.gamePlayer.nickName;

			var goldText = useInfo.FindChildDeeply("UserMoney").GetComponent<TextMeshProUGUI>();
			goldText.text = App.ins.self.gamePlayer.items[(int)ITEMID.GOLD].ShowAsGold();
		}

		protected override void OnClose()
		{
			betItems_.Clear();
			App.ins.self.gamePlayer.onDataChanged -= OnMyDataChanged;
			base.Close();
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

		public void ResetBetState_()
		{
			foreach (var bi in betItems_) {
				bi.Value.SetMybet(0);
				bi.Value.SetTotalBet(0);
			}

			for(int i = 1; i <= 8; i++) {
				var winStage = GameObject.Find("winStage").FindChildDeeply((i).ToString());
				winStage.SetActive(true);
				winStage.StartParticles();
			}

			foreach(var obj in flyingChips) {
				GameObject.Destroy(obj);
			}
			flyingChips.Clear();

			carWinFx.SetActive(false);
		}

		void ResetResultPanel_()
		{
			var selfInfo = resultPanel.FindChildDeeply("selfInfo");
			var NOBetTip = resultPanel.FindChildDeeply("NOBetTip");
			var winScore = selfInfo.FindChildDeeply("winScore");
			NOBetTip.SetActive(false);
			winScore.SetActive(false);

			var name = selfInfo.FindChildDeeply("name").GetComponent<Text>();
			name.text = App.ins.self.gamePlayer.nickName;

			for (int i = 0; i < 5; i++) {
				var otherInfo = resultPanel.FindChildDeeply("otherInfo_" + (i + 1));
				otherInfo.SetActive(false);
			}

		}

		IEnumerator RandomShine()
		{
			var carIndexs = GameObject.Find("carIndexs");
			while (st == GameControllerBase.GameState.state_do_random) {
				int i = Globals.Random_Range(1, 32);
				var obj = carIndexs.FindChildDeeply(i.ToString()).FindChildDeeply("select");
				obj.StartParticles();
				yield return new WaitForSeconds(0.1f);
			}
		}

		public override void OnStateChange(msg_state_change msg)
		{
			st = (GameControllerBase.GameState)int.Parse(msg.change_to_);
			var txtCounter = canvas.FindChildDeeply("GameTimeCounter").FindChildDeeply("Time").GetComponent<TextMeshProUGUI>();
			var gameStateText = canvas.FindChildDeeply("gameStateText").FindChildDeeply("gameStateText").GetComponent<TextMeshProUGUI>();

			var StartBet =  canvas.FindChildDeeply("StartBet");
			var StopBet = canvas.FindChildDeeply("StopBet");

			if (int.Parse(msg.time_total_) > 0)
				stateTimePercent = int.Parse(msg.time_left) * 1.0f / int.Parse(msg.time_total_);

			StartBet.SetActive(false);
			StopBet.SetActive(false);
			
			if (st == GameControllerBase.GameState.state_wait_start) {
				myTotalBet_ = 0;
				resultPanel.SetActive(false);
				gameStateText.text = LangMultiplayer.WaitingForBet;
				waitNextStateTip.SetActive(false);
				
				StartBet.SetActive(true);
				StartBet.StartSpine();

				if(!App.ins.currentApp.game.isEntering)
					ResetBetState_();

				gameReports.Clear();

				Globals.cor.RunAction(this, 2.0f * stateTimePercent, () => {
					StartBet.SetActive(false);
				});


				this.StartCor(RandomShine(), false);

			}
			else if (st == GameControllerBase.GameState.state_do_random) {
				for (int i = 1; i <= 8; i++) {
					var winStage = GameObject.Find("winStage").FindChildDeeply((i).ToString());
					winStage.SetActive(false);
					winStage.StopParticles();
				}

				gameStateText.text = LangMultiplayer.RandomResult;
				resultPanel.SetActive(false);

				StopBet.SetActive(true);
				StopBet.StartSpine();

				Globals.cor.RunAction(this, 2.0f * stateTimePercent, () => {
					StopBet.SetActive(false);
				});

			}
			else if (st == GameControllerBase.GameState.state_rest_end) {
				gameStateText.text = LangMultiplayer.BalanceResult;
				resultPanel.SetActive(true);
				ResetResultPanel_();
			}

			this.StartCor(CountDown_(int.Parse(msg.time_left), txtCounter), false);
		}

		public override void OnPlayerSetBet(msg_player_setbet msg)
		{
			var view = GameObject.Find("View");
			var bi = betItems_[int.Parse(msg.present_id_)];
			
			bi.SetTotalBet(long.Parse(msg.max_setted_));

			var flyOutPos = view.FindChildDeeply("flyOutPos");

			int chipID = int.Parse(msg.pid_);
			FlyChipTo(msg.present_id_, chipID, flyOutPos.transform.position);
		}

		void FlyChipTo(string presentID, int chipID, Vector3 startPos)
		{
			var view = GameObject.Find("View");
			var dstPosList = view.FindChildDeeply("dstPosList");
			var betTarget = dstPosList.FindChildDeeply("betArea" + presentID);
			var chipToFly = GameObject.Instantiate(chipModels[chipID - 1]);
			chipToFly.transform.SetParent(betTarget.transform, true);

			if (startPos.x == float.PositiveInfinity)
				chipToFly.transform.position = chipModels[chipID - 1].transform.position;
			else
				chipToFly.transform.position = startPos;

			var pos = betTarget.transform.position;
			pos.x += Globals.Random_Range(-100, 100) / 20.0f;
			pos.z += Globals.Random_Range(-100, 100) / 30.0f;

			chipToFly.transform.DOMove(pos, 0.3f);
			flyingChips.Add(chipToFly);
			//筹码太多了需要移除
			Globals.cor.RunAction(this, 0.1f, () => {
				if(betTarget.transform.childCount > 10) {
					var obj = betTarget.transform.GetChild(0);
					GameObject.Destroy(obj.gameObject);
					flyingChips.Remove(obj.gameObject);
				}
			});
		}

		public override void OnMyBet(msg_my_setbet msg)
		{
			if (App.ins.currentApp.game.isEntering) {
				MyDebug.LogFormat("OnMyBet in entering.");
			}
			var bi = betItems_[int.Parse(msg.present_id_)];
			bi.SetMybet(long.Parse(msg.my_total_set_));
			bi.SetTotalBet(long.Parse(msg.total_set_));
			myTotalBet_ += long.Parse(msg.set_);
			int chipID = int.Parse(msg.pid_);
			FlyChipTo(msg.present_id_, chipID, Vector3.positiveInfinity);
		}

		public override GamePlayer OnPlayerEnter(msg_player_seat msg)
		{
			var pp = base.OnPlayerEnter(msg);
			return pp;
		}

		public override void OnPlayerLeave(msg_player_leave msg)
		{
			var game = App.ins.currentApp.game;
			var pp = game.GetPlayer(msg.pos_);
			game.RemovePlayer(int.Parse(msg.pos_));
		}

		IEnumerator DoRandomResult_(msg_random_result_base msg, bool setRecord)
		{
			MyApp app = (MyApp)App.ins.currentApp;
			var pmsg = (msg_random_result)msg;
			int result = int.Parse(pmsg.rand_result_);
			var bi = app.conf.itemsPlaced[result];

			
			var winIcon = resultPanel.FindChildDeeply("winIcon").GetComponent<Image>();
			winIcon.ChangeSprite(resultItemTexture[(int)bi]);

			var winRatio = resultPanel.FindChildDeeply("winRatio").GetComponent<TextMeshProUGUI>();
			winRatio.text = "X" + ((MyApp)App.ins.currentApp).conf.ratio[bi];
			
			if(st == GameControllerBase.GameState.state_do_random) {
				thisPid_ = result;

				List<int> wayPointHolder = new List<int>();

				//上次停的位置距离原点
				for(int i = lastPointerPos_; i <= 32; i ++) {
					wayPointHolder.Add(i);
				}

				//中间跑5圈
				for(int j = 0; j < 5; j++) {
					for(int i = 1; i <= 32; i++) {
						wayPointHolder.Add(i);
					}
				}

				//本次目标点
				for (int i = 1; i <= thisPid_; i++) {
					wayPointHolder.Add(i);
				}

				lastPointerPos_ = thisPid_;

				var paochePathList = GameObject.Find("paochePathList");
				var carIndexs = GameObject.Find("carIndexs");
				List<Vector3> wps = new List<Vector3>();
				List<GameObject> objs = new List<GameObject>();
				for (int i = 0; i < wayPointHolder.Count; i++) {
					var obj = paochePathList.FindChildDeeply(wayPointHolder[i].ToString());
					wps.Add(obj.transform.position);
					obj = carIndexs.FindChildDeeply(wayPointHolder[i].ToString());
					objs.Add(obj);
				}

				yield return car.StartRace(stateTimePercent);

				var tween = car.carModel.transform.DOPath(wps.ToArray(), app.conf.carRunnigTime * stateTimePercent, PathType.CatmullRom);
				tween.SetLookAt(0.01f);
				tween.SetEase(Ease.InOutQuint);
				
				tween.onWaypointChange = (index) => {
					if(index == 5) {
						car.Running();
					}
					else if(objs.Count - index == 10) {
						car.AboutToEnd();
					}
					else if (objs.Count - index == 2) {
						car.Stoping();
					}
					var current = objs[index];
					current.StartParticles();
				};

				yield return new WaitForSeconds(app.conf.carRunnigTime * stateTimePercent);
				car.Stoped();

				var winStage = GameObject.Find("winStage").FindChildDeeply(((int)bi).ToString());
				winStage.SetActive(true);
				winStage.StartParticles();

				carWinFx.SetActive(true);
				carWinFx.transform.position = objs[objs.Count - 1].transform.position;
			}
			
			if (setRecord) {
				var rec = CreateGameRecordItem_(result);
				recordViewport.AddChild(rec);
				if (recordViewport.transform.childCount > app.conf.maxRecordCount) {
					var t = recordViewport.transform.GetChild(0);
					GameObject.Destroy(t.gameObject);
				}
			}

			yield return 0;
		}

		public override void OnRandomResult(msg_random_result_base msg)
		{
			MyApp app = (MyApp)App.ins.currentApp;
			var pmsg = (msg_random_result)msg;
			int result = int.Parse(pmsg.rand_result_);
			var bi = app.conf.itemsPlaced[result];
			bool setRecord = lastTurn_ < int.Parse(msg.turn_);
			MyDebug.LogFormat("RandomResult:{0},{1},{2}", result, bi, app.conf.itemsPlaced[result]);

			this.StartCor(DoRandomResult_(msg, setRecord), true);
		}

		GameObject CreateGameRecordItem_(int pid)
		{
			MyApp app = (MyApp)App.ins.currentApp;
			if (!app.conf.itemsPlaced.ContainsKey(pid)) {
				MyDebug.LogWarning("pid not find:" + pid.ToString());
			}
			var item = app.conf.itemsPlaced[pid];
			GameObject obj = new GameObject();
			var img = obj.AddComponent<Image>();
			img.ChangeSprite(rewardItemTexture[(int)item]);
			return obj;
		}

		public override void OnLastRandomResult(msg_last_random_base msg)
		{
			var pmsg = (msg_last_random)msg;
			List<int> pids = Globals.Split(pmsg.r_, ",");
			List<int> turns = Globals.Split(pmsg.t_, ",");

			if (pids.Count == 0 || turns.Count == 0) return;

			//服务器是最新的在新前面,需要倒过来显示
			for (int i = pids.Count - 1; i >= 0; i--) {
				var rec = CreateGameRecordItem_(pids[i]);

				recordViewport.AddChild(rec);
			}
			lastTurn_ = turns.First();
		}

		public override void OnBankDepositChanged(msg_banker_deposit_change msg)
		{
			if (banker == null) return;
			var BankerInfo = canvas.FindChildDeeply("BankerInfo");

			var BankerMoney = BankerInfo.FindChildDeeply("BankerMoney").GetComponent<TextMeshProUGUI>();
			BankerMoney.text = msg.credits_;

			var bankerInfo = resultPanel.FindChildDeeply("bankerInfo");
			var name = bankerInfo.FindChildDeeply("name").GetComponent<Text>();
			name.text = banker.nickName;
			var txtBankerWin = bankerInfo.FindChildDeeply("winScore").GetComponent<Text>();

			long bankerWin = long.Parse(msg.this_win_);
			if(bankerWin > 0) {
				txtBankerWin.font = fontWin;
				txtBankerWin.text = "+" + bankerWin.ShowAsGold();
			}
			else {
				txtBankerWin.font = fontLose;
				txtBankerWin.text = bankerWin.ShowAsGold();
			}
		}

		//庄家信息
		public override void OnBankPromote(msg_banker_promote msg)
		{
			if(banker != null) {
				var ChangeBankerTip = canvas.FindChildDeeply("ChangeBankerTip");
				ChangeBankerTip.StartAnim();
			}

			banker = App.ins.currentApp.game.GetPlayer(msg.uid_);
			if(banker == null) {
				banker = OnPlayerEnter(msg);
			}
			
			var BankerInfo = canvas.FindChildDeeply("BankerInfo");
			var BankerProfile = BankerInfo.FindChildDeeply("BankerProfile").FindChildDeeply("BankerProfile").GetComponent<Image>();
			banker.SetHeadPic(BankerProfile);
			var BankerProfileFrame = BankerInfo.FindChildDeeply("BankerProfileFrame").GetComponent<Image>();
			banker.SetHeadFrame(BankerProfileFrame);
		
			var BankerName = BankerInfo.FindChildDeeply("BankerName").GetComponent<TextMeshProUGUI>();
			if (int.Parse(msg.is_sys_banker_) == 0) {
				BankerName.text = Language.gameName;
			}
			else {
				BankerName.text = banker.nickName;
			}

			var BankerMoney = BankerInfo.FindChildDeeply("BankerMoney").GetComponent<TextMeshProUGUI>();
			BankerMoney.text = msg.deposit_;
		}

		public override void OnGameInfo(msg_game_info msg)
		{
			turn_ = int.Parse(msg.turn_);
		}

		public override void OnGameReport(msg_game_report msg)
		{
			if (msg.uid_ == App.ins.self.gamePlayer.uid) {
				var selfInfo = resultPanel.FindChildDeeply("selfInfo");
				var winScore = selfInfo.FindChildDeeply("winScore");
				long win = long.Parse(msg.actual_win_);
				if(win > 0) {
					winScore.GetComponent<Text>().font = fontWin;
					winScore.GetComponent<Text>().text = "+" + win.ShowAsGold();
				}
				else {
					winScore.GetComponent<Text>().font = fontLose;
					winScore.GetComponent<Text>().text = win.ShowAsGold();
				}
				var NOBetTip = resultPanel.FindChildDeeply("NOBetTip");
				NOBetTip.SetActive(false);
				winScore.SetActive(false);
				if (msg.pay_ == "0") {
					NOBetTip.SetActive(true);
				}
				else {
					winScore.SetActive(true);
				}

				var loseBG = resultPanel.FindChildDeeply("loseBG");
				var winBG = resultPanel.FindChildDeeply("winBG");
				loseBG.SetActive(false);
				winBG.SetActive(false);
				if (win < 0) {
					loseBG.SetActive(true);
					loseBG.StartSpine();
				}
				else {
					winBG.SetActive(true);
					winBG.StartSpine();
				}
			}
			else {
				gameReports.Add(msg);
			}

			gameReports.Sort((it1, it2) => { 
				long dt = long.Parse(it2.actual_win_) - long.Parse(it1.actual_win_);
				return (int)dt;
			});

			for (int i = 0; i < gameReports.Count && i < 5; i++) {
				var thisMsg = gameReports[i];
				
				var otherInfo_1 = resultPanel.FindChildDeeply("otherInfo_" + (i + 1));
				otherInfo_1.SetActive(true);

				var txtName = otherInfo_1.FindChildDeeply("name").GetComponent<Text>();
				var txtwinScore = otherInfo_1.FindChildDeeply("winScore").GetComponent<Text>();
				txtName.text = thisMsg.nickname_;
				long v = long.Parse(thisMsg.actual_win_);
				txtwinScore.text = v.ShowAsGold();							
			}
		}

		public override void OnGoldChange(msg_deposit_change2 msg)
		{
			int pos = App.ins.self.gamePlayer.serverPos;
			if (int.Parse(msg.pos_) == pos) {
				if(int.Parse(msg.display_type_) == (int)msg_deposit_change2.dp.display_type_sync_gold) {
					App.ins.self.gamePlayer.items.SetKeyVal((int)ITEMID.GOLD, long.Parse(msg.credits_));
					App.ins.self.gamePlayer.DispatchDataChanged();
				}
			}
		}

		public override void OnGoldChange(msg_currency_change msg)
		{
			if(msg.why_ == "0") {
				App.ins.self.gamePlayer.items.SetKeyVal((int)ITEMID.GOLD, long.Parse(msg.credits_));
				App.ins.self.gamePlayer.DispatchDataChanged();
			}

		}

		public override void OnApplyBanker(msg_new_banker_applyed msg)
		{
			if (!applyList_.ContainsKey(msg.uid_)) {
				applyList_.Add(msg.uid_, msg);
				var WaitingBankerName = canvas.FindChildDeeply("WaitingBankerName").GetComponent<TextMeshProUGUI>();
				WaitingBankerName.text = string.Format(Language.ApplyingBanker, applyList_.Count);
			}

			//如果我在上庄
			if (applyList_.ContainsKey(App.ins.self.gamePlayer.uid)) {
				var txt = btnToBanker.FindChildDeeply("Text (TMP)").GetComponent<TextMeshProUGUI>();
				txt.text = Language.CancelBanker;
				isApplyingBanker = true;
			}
		}

		public override void OnCancelBanker(msg_apply_banker_canceled msg)
		{
			applyList_.Remove(msg.uid_);
			var WaitingBankerName = canvas.FindChildDeeply("WaitingBankerName").GetComponent<TextMeshProUGUI>();
			WaitingBankerName.text = string.Format(Language.ApplyingBanker, applyList_.Count);
			//如果我不在上庄
			if (!applyList_.ContainsKey(App.ins.self.gamePlayer.uid)) {
				var txt = btnToBanker.FindChildDeeply("Text (TMP)").GetComponent<TextMeshProUGUI>();
				txt.text = Language.ApplyBanker;
				isApplyingBanker = false;
			}
		}

		public override void OnCommonReply(msg_common_reply msg)
		{
			int cmd = int.Parse(msg.rp_cmd_);
			int err = int.Parse(msg.err_);

			if(cmd == (int)GameMultiReqID.msg_apply_banker_req) {
				if(err == 2005) {
					ViewToast.Create(Language.BankerRequirement);
				}
			}
		}

		public override void OnJackpotNumber(msg_get_public_data_ret msg)
		{
			throw new NotImplementedException();
		}

		long myTotalBet_
		{
			get {
				return myTotalBet__;
			}
			set {
				myTotalBet__ = value;
			}
		}

		public Font fontWin, fontLose;

		public GameObject betStageRoot, waitingBankerName,
			canvas, resultPanel, recordViewport, canvas3D,
			announcementFire, waitNextStateTip, btnToBanker, carWinFx;

		public int betSelected = 1, turn_ = 0, lastBetTurn_ = -1;
		public List<msg_set_bets_req> lastBets = new List<msg_set_bets_req>();
		Dictionary<int, BetItem> betItems_ = new Dictionary<int, BetItem>();

		int lastPointerPos_ = 1, lastTurn_ = 0, thisPid_ = 0;
		Dictionary<int,Texture2D> rewardItemTexture = new Dictionary<int, Texture2D>();
		Dictionary<int, Texture2D> resultItemTexture = new Dictionary<int, Texture2D>();
		float stateTimePercent = 0.0f;
		long myTotalBet__ = 0;

		//用来排序
		List<msg_game_report> gameReports = new List<msg_game_report>();
		GamePlayer banker;
		bool isAutoBeting = false, isApplyingBanker = false;
		Dictionary<string, msg_new_banker_applyed> applyList_ = new Dictionary<string, msg_new_banker_applyed>();
		RunningCar car;

		List<GameObject> chipModels = new List<GameObject>();
		List<GameObject> flyingChips = new List<GameObject>();
	}
}
