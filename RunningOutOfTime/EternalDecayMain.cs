using RunningOutOfTime.Content.Config;
using UnityEngine;

namespace RunningOutOfTime
{
    internal class EternalDecayMain : KMonoBehaviour
    {
        // 这里的配置字段保持不变
        public static float MinionAgeThreshold = TUNINGS.AGE.MINION_AGE_THRESHOLD;

        // 通知方法保持静态，方便组件调用
        public static void NotifyDeathApplied(GameObject gameObject)
        {
            Notifier notifier = gameObject.AddOrGet<Notifier>();
            Notification notification = new Notification(
                Content.Config.STRINGS.MISC.NOTIFICATIONS.DEATHROULETTE.NAME,
                NotificationType.BadMinor,
                (notificationList, data) => notificationList.ReduceMessages(false),
                "/t• " + gameObject.GetProperName(),
                true
            );
            notifier.Add(notification, "");
        }
    }
}