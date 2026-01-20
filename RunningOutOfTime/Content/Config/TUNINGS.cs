using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RunningOutOfTime.Content.Config
{
    public class TUNINGS
    {
        public class AGE
        {
            public static float MINION_AGE_THRESHOLD = 2f; // 复制人年龄阈值（单位：周期）
            public static float AGE_80PERCENT_THRESHOLD = 0.8f; // 年龄80%阈值

        }


        public class TIMERMANAGER
        {
            public class RANDOMDEBUFFTIMERMANAGER
            {
                public class TRANSFER
                {
                    public static int TRAITSMAXAMOUNT = 12; // 最大特质继承数量
                    public static int ATTRIBUTEMAXLEVEL = 50; // 最大属性等级
                    public static int SKILLMAXAMOUNT = 17; // 最大技能继承数量
                }

            }
        }
        public class SKILLS
        {
            public const int TARGET_SKILLS_EARNED = 20; // 目标技能点数
            public const float EXPERIENCE_LEVEL_POWER = 1.5f; // 经验等级提升幂次
            public const float TARGET_SKILLS_CYCLE = 10f; // 每周期目标技能点数
        }
    }
}
