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
		protected override IEnumerator OnStart()
		{
			yield return base.OnStart();
			yield return ShowLoading_();
		}

		public override IEnumerator ShowLogin()
		{
			MyDebug.Log("ShowLogin()");
			yield return base.ShowLogin();
		}

		IEnumerator ShowLoading_()
		{
			var loading = CreateLoading();
			OpenView(loading);
			yield return 0;
		}

		public override msg_random_result_base CreateRandomResult(string json)
		{
			return JsonMapper.ToObject<msg_random_result>(json);
		}

		public override msg_last_random_base CreateLastRandom(string json)
		{
			return JsonMapper.ToObject<msg_last_random>(json);
		}
		protected override ViewGameSceneBase OnCreateViewGameScene(IShowDownloadProgress loadingProgress)
		{
			return new ViewGameScene(loadingProgress);
		}

		protected override ViewLoadingBase OnCreateLoading(IShowDownloadProgress loadingProgress)
		{
			return new ViewLoading(loadingProgress);
		}
	}
}
