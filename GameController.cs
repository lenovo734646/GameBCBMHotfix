using AssemblyCommon;
using Hotfix.Common;
using Hotfix.Common.MultiPlayer;
using LitJson;
using System.Collections;
using UnityEngine;

namespace Hotfix.BCBM
{
	public class GameController : GameControllerMultiplayer
	{
		public ViewLoading loading;
		public override void Start()
		{
			base.Start();
			this.StartCor(ShowLoading_(), false);
		}

		public override IEnumerator ShowLogin()
		{
			MyDebug.Log("ShowLogin()");
			return base.ShowLogin();
		}

		IEnumerator ShowLoading_()
		{
			loading = new ViewLoading(null);
			OpenView(loading);
			yield return 0;
		}

		//调用这个避免内存泄露
		public override void Stop()
		{
			//移除所有正在执行的协程.
			this.StopCor(-1);
			base.Stop();
		}

		IEnumerator DoLoadMainScene()
		{
			yield return loading.WaitingForReady();
			var view = new ViewGameScene(loading.loading);
			OpenView(view);
			yield return 0;
		}

		protected override IEnumerator OnGameLoginSucc()
		{
			yield return DoLoadMainScene();

			var handle1 = App.ins.network.CoEnterGameRoom(0, 0);
			yield return handle1;
			if ((int)handle1.Current == 0) {
				ViewToast.Create(LangNetWork.EnterRoomFailed);
			}
		}

		public override msg_random_result_base CreateRandomResult(string json)
		{
			return JsonMapper.ToObject<msg_random_result>(json);
		}

		public override msg_last_random_base CreateLastRandom(string json)
		{
			return JsonMapper.ToObject<msg_last_random>(json);
		}
	}
}
