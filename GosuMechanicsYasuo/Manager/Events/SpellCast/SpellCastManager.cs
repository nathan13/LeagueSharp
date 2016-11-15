﻿namespace GosuMechanicsYasuo.Manager.Events
{
    using LeagueSharp;
    using LeagueSharp.Common;

    internal class SpellCastManager : Logic
    {
        public static void Init(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs Args)
        {
            if (sender.IsMe)
            {
                if (Args.SData.Name == "YasuoDashWrapper" && Args.Target != null && !Args.Target.IsAlly)
                {
                    var target = (Obj_AI_Base)Args.Target;

                    lastEPos = Common.Common.PosAfterE(target).To3D();
                    lastECast = Utils.TickCount;
                }
            }
        }
    }
}