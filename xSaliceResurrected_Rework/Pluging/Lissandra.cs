﻿namespace xSaliceResurrected_Rework.Pluging
{
    using System;
    using System.Linq;
    using LeagueSharp;
    using LeagueSharp.Common;
    using SharpDX;
    using Base;
    using Managers;
    using Utilities;
    using Color = System.Drawing.Color;
    using Geometry = LeagueSharp.Common.Geometry;
    using Orbwalking = Orbwalking;

    internal class Lissandra : Champion
    {
        private MissileClient _eMissle;
        private bool _eCreated;

        public Lissandra()
        {
            SpellManager.Q = new Spell(SpellSlot.Q, 725);
            SpellManager.QExtend = new Spell(SpellSlot.Q, 850);
            SpellManager.W = new Spell(SpellSlot.W, 450);
            SpellManager.E = new Spell(SpellSlot.E, 1050);
            SpellManager.R = new Spell(SpellSlot.R, 700);

            SpellManager.Q.SetSkillshot(0.50f, 100, 1300, false, SkillshotType.SkillshotLine);
            SpellManager.QExtend.SetSkillshot(0.50f, 150, 1300, true, SkillshotType.SkillshotLine);
            SpellManager.W.SetSkillshot(0.25f, 450, float.MaxValue, false, SkillshotType.SkillshotCircle);
            SpellManager.E.SetSkillshot(0.50f, 110, 850, false, SkillshotType.SkillshotLine);

            SpellManager.SpellList.Add(Q);
            SpellManager.SpellList.Add(W);
            SpellManager.SpellList.Add(E);
            SpellManager.SpellList.Add(R);

            var combo = new Menu("Combo", "Combo");
            {
                combo.AddItem(new MenuItem("UseQCombo", "Use Q", true).SetValue(true));
                combo.AddItem(new MenuItem("UseWCombo", "Use W", true).SetValue(true));
                combo.AddItem(new MenuItem("UseECombo", "Use E", true).SetValue(true));
                combo.AddItem(new MenuItem("UseRCombo", "Use R", true).SetValue(true));
                combo.AddItem(new MenuItem("alwaysR", "Always Cast R", true).SetValue(new KeyBind("H".ToCharArray()[0], KeyBindType.Toggle)));
                combo.AddItem(new MenuItem("rHp", "R if HP <", true).SetValue(new Slider(20)));
                combo.AddItem(new MenuItem("defR", "R Self if > enemy", true).SetValue(new Slider(3, 0, 5)));
                Menu.AddSubMenu(combo);
            }

            var harass = new Menu("Harass", "Harass");
            {
                harass.AddItem(new MenuItem("UseQHarass", "Use Q", true).SetValue(true));
                harass.AddItem(new MenuItem("UseWHarass", "Use W", true).SetValue(false));
                harass.AddItem(new MenuItem("UseEHarass", "Use E", true).SetValue(true));
                harass.AddItem(new MenuItem("FarmT", "Harass (toggle)!", true).SetValue(new KeyBind("Y".ToCharArray()[0], KeyBindType.Toggle)));
                ManaManager.AddManaManagertoMenu(harass, "Harass", 30);
                Menu.AddSubMenu(harass);
            }

            var farm = new Menu("Farm", "Farm");
            {
                farm.AddItem(new MenuItem("UseQFarm", "Use Q", true).SetValue(false));
                farm.AddItem(new MenuItem("UseWFarm", "Use W", true).SetValue(false));
                farm.AddItem(new MenuItem("UseEFarm", "Use E", true).SetValue(false));
                ManaManager.AddManaManagertoMenu(harass, "Farm", 30);
                Menu.AddSubMenu(farm);
            }

            var lastHit = new Menu("LastHit", "LastHit");
            {
                lastHit.AddItem(new MenuItem("UseQLastHit", "Use Q", true).SetValue(true));
                ManaManager.AddManaManagertoMenu(harass, "LastHit", 30);
                Menu.AddSubMenu(lastHit);
            }

            var misc = new Menu("Misc", "Misc");
            {
                misc.AddSubMenu(AoeSpellManager.AddHitChanceMenuCombo(true, true, true, false));
                misc.AddItem(new MenuItem("stunMelles", "Stun Enemy Melle Range", true).SetValue(new KeyBind("M".ToCharArray()[0], KeyBindType.Toggle)));
                misc.AddItem(new MenuItem("stunTowers", "Stun Enemy under Tower", true).SetValue(new KeyBind("J".ToCharArray()[0], KeyBindType.Toggle)));
                misc.AddItem(new MenuItem("UseInt", "Use R to Interrupt", true).SetValue(true));
                misc.AddItem(new MenuItem("UseGap", "Use W for GapCloser", true).SetValue(true));
                misc.AddItem(new MenuItem("smartKS", "Use Smart KS System", true).SetValue(true));
                misc.AddItem(new MenuItem("UseHAM", "Always use E", true).SetValue(new KeyBind("I".ToCharArray()[0], KeyBindType.Toggle)));
                misc.AddItem(new MenuItem("UseEGap", "Use E to Gap Close", true).SetValue(true));
                misc.AddItem(new MenuItem("gapD", "Min Distance", true).SetValue(new Slider(600, 300, 1050)));
                Menu.AddSubMenu(misc);
            }

            var drawing = new Menu("Drawings", "Drawings");
            {
                drawing.AddItem(new MenuItem("QRange", "Q range", true).SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
                drawing.AddItem(new MenuItem("qExtend", "Extended Q range", true).SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
                drawing.AddItem(new MenuItem("WRange", "W range", true).SetValue(new Circle(true, Color.FromArgb(100, 255, 0, 255))));
                drawing.AddItem(new MenuItem("ERange", "E range", true).SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
                drawing.AddItem(new MenuItem("RRange", "R range", true).SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));

                var drawComboDamageMenu = new MenuItem("Draw_ComboDamage", "Draw Combo Damage", true).SetValue(true);
                var drawFill = new MenuItem("Draw_Fill", "Draw Combo Damage Fill", true).SetValue(new Circle(true, Color.FromArgb(90, 255, 169, 4)));
                drawing.AddItem(drawComboDamageMenu);
                drawing.AddItem(drawFill);
                DamageIndicator.DamageToUnit = GetComboDamage;
                DamageIndicator.Enabled = drawComboDamageMenu.GetValue<bool>();
                DamageIndicator.Fill = drawFill.GetValue<Circle>().Active;
                DamageIndicator.FillColor = drawFill.GetValue<Circle>().Color;
                drawComboDamageMenu.ValueChanged +=
                    delegate (object sender, OnValueChangeEventArgs eventArgs)
                    {
                        DamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
                    };
                drawFill.ValueChanged +=
                    delegate (object sender, OnValueChangeEventArgs eventArgs)
                    {
                        DamageIndicator.Fill = eventArgs.GetNewValue<Circle>().Active;
                        DamageIndicator.FillColor = eventArgs.GetNewValue<Circle>().Color;
                    };

                Menu.AddSubMenu(drawing);
            }

            var customMenu = new Menu("Custom Perma Show", "Custom Perma Show");
            {
                var myCust = new CustomPermaMenu();
                customMenu.AddItem(new MenuItem("custMenu", "Move Menu", true).SetValue(new KeyBind("L".ToCharArray()[0], KeyBindType.Press)));
                customMenu.AddItem(new MenuItem("enableCustMenu", "Enabled", true).SetValue(true));
                customMenu.AddItem(myCust.AddToMenu("Combo Active: ", "Orbwalk"));
                customMenu.AddItem(myCust.AddToMenu("Harass Active: ", "Farm"));
                customMenu.AddItem(myCust.AddToMenu("Laneclear Active: ", "LaneClear"));
                customMenu.AddItem(myCust.AddToMenu("LastHit Q Active: ", "LastHitQQ"));
                customMenu.AddItem(myCust.AddToMenu("StunMelle Active: ", "stunMelles"));
                customMenu.AddItem(myCust.AddToMenu("StunTower Active: ", "stunTowers"));
                customMenu.AddItem(myCust.AddToMenu("Always R Active: ", "alwaysR"));
                customMenu.AddItem(myCust.AddToMenu("Always E Active: ", "UseHAM"));
                Menu.AddSubMenu(customMenu);
            }
        }

        private float GetComboDamage(Obj_AI_Base enemy)
        {
            var damage = 0d;

            if (Q.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.Q) * 2;

            if (W.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.W);

            if (E.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.E);

            if (R.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.R);

            damage = ItemManager.CalcDamage(enemy, damage);

            return (float)damage;
        }

        private void Combo()
        {
            var qTarget = TargetSelector.GetTarget(QExtend.Range, TargetSelector.DamageType.Magical);
            var eTarget = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);

            if (Menu.Item("UseECombo", true).GetValue<bool>() && eTarget != null && 
                E.IsReady() && Player.Distance(eTarget.Position) < E.Range && ShouldE(eTarget))
            {
                SpellCastManager.CastBasicSkillShot(E, E.Range, TargetSelector.DamageType.Magical, HitChance.VeryHigh);
            }

            if (Menu.Item("UseRCombo", true).GetValue<bool>() && qTarget != null &&
                R.IsReady() && Player.Distance(qTarget.Position) < R.Range)
            {
                CastR(qTarget);
            }

            var itemTarget = TargetSelector.GetTarget(750, TargetSelector.DamageType.Physical);

            if (itemTarget != null)
            {
                var dmg = GetComboDamage(itemTarget);

                ItemManager.Target = itemTarget;

                if (dmg > itemTarget.Health - 50)
                    ItemManager.KillableTarget = true;

                ItemManager.UseTargetted = true;
            }

            if (Menu.Item("UseWCombo", true).GetValue<bool>() && qTarget != null && W.IsReady())
            {
                if (W.GetPrediction(qTarget).Hitchance > HitChance.High && Player.Distance(qTarget.Position) <= W.Width)
                    W.Cast();
            }

            if (Menu.Item("UseQCombo", true).GetValue<bool>() && Q.IsReady() && qTarget != null)
            {
                CastQ();
            }
        }

        private void Harass()
        {
            if (!ManaManager.HasMana("Harass"))
                return;

            var qTarget = TargetSelector.GetTarget(QExtend.Range, TargetSelector.DamageType.Magical);
            var eTarget = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);

            if (Menu.Item("UseEHarass", true).GetValue<bool>() && eTarget != null && E.IsReady() && Player.Distance(eTarget.Position) < E.Range && ShouldE(eTarget))
            {
                SpellCastManager.CastBasicSkillShot(E, E.Range, TargetSelector.DamageType.Magical, HitChance.VeryHigh);
            }

            if (Menu.Item("UseWHarass", true).GetValue<bool>() && qTarget != null && W.IsReady())
            {
                if (W.GetPrediction(qTarget).Hitchance > HitChance.High && Player.Distance(qTarget.Position) <= W.Width)
                    W.Cast();
            }

            if (Menu.Item("UseQHarass", true).GetValue<bool>() && Q.IsReady() && qTarget != null)
            {
                CastQ();
            }
        }

        private void CastQ()
        {
            if (!Q.IsReady())
                return;

            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

            if (target != null && target.IsValidTarget(Q.Range))
            {
                SpellCastManager.CastBasicSkillShot(Q, Q.Range, TargetSelector.DamageType.Magical, HitChance.VeryHigh);
            }

            target = TargetSelector.GetTarget(QExtend.Range, TargetSelector.DamageType.Physical);

            if (target == null)
                return;

            var pred = QExtend.GetPrediction(target, true);
            var collisions = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly);

            if (!collisions.Any())
                return;

            foreach (var minion in collisions)
            {
                var poly = new Geometry.Polygon.Rectangle(Player.ServerPosition, Player.ServerPosition.Extend(minion.ServerPosition, QExtend.Range), QExtend.Width);

                if (poly.IsInside(pred.UnitPosition))
                {
                    if (Q.Cast(minion) == Spell.CastStates.SuccessfullyCasted)
                    {
                        Q.LastCastAttemptT = Utils.TickCount;
                        return;
                    }
                }
            }
        }

        private bool ShouldE(Obj_AI_Hero target)
        {
            if (_eCreated)
                return false;

            if (GetComboDamage(target) >= target.Health + 20)
                return true;

            return Menu.Item("UseHAM", true).GetValue<KeyBind>().Active;
        }

        private void CastR(Obj_AI_Hero target)
        {
            if (Menu.Item("alwaysR", true).GetValue<KeyBind>().Active)
            {
                R.Cast(target);
                return;
            }

            if (GetComboDamage(target) > target.Health + 20)
            {
                R.Cast(target);
                return;
            }

            if ((Player.GetSpellDamage(target, SpellSlot.R) * 1.2) > target.Health + 20)
            {
                R.Cast(target);
                return;
            }

            var rHp = Menu.Item("rHp", true).GetValue<Slider>().Value;
            var hpPercent = Player.Health / Player.MaxHealth * 100;

            if (hpPercent < rHp)
            {
                R.CastOnUnit(Player);
                return;
            }

            var rDef = Menu.Item("defR", true).GetValue<Slider>().Value;

            if (Player.CountEnemiesInRange(300) >= rDef)
            {
                R.CastOnUnit(Player);
            }
        }

        private void SmartKs()
        {
            if (!Menu.Item("smartKS", true).GetValue<bool>())
                return;

            foreach (var target in HeroManager.Enemies.Where(x => x.IsValidTarget(1000) && !x.HasBuffOfType(BuffType.Invulnerability)).OrderByDescending(GetComboDamage))
            {
                if (Player.Distance(target.ServerPosition) <= Q.Range && 
                    Player.GetSpellDamage(target, SpellSlot.Q) > target.Health + 20)
                {
                    if (Q.IsReady())
                    {
                        Q.Cast(target);
                        return;
                    }
                }

                if (Player.Distance(target.ServerPosition) <= E.Range && 
                    Player.GetSpellDamage(target, SpellSlot.E) > target.Health + 20)
                {
                    if (E.IsReady() && E.GetPrediction(target).Hitchance >= HitChance.High)
                    {
                        E.Cast(target);
                        return;
                    }
                }

                if (Player.Distance(target.ServerPosition) <= W.Width && 
                    Player.GetSpellDamage(target, SpellSlot.W) > target.Health + 20)
                {
                    if (W.IsReady())
                    {
                        W.Cast();
                        return;
                    }
                }
            }
        }

        private void DetonateE()
        {
            var enemy = TargetSelector.GetTarget(2000, TargetSelector.DamageType.Magical);
            if (_eMissle == null || !enemy.IsValidTarget(2000))
                return;

            if (enemy.ServerPosition.Distance(_eMissle.Position) < 110 && _eCreated &&
                Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && E.IsReady())
            {
                E.Cast();
            }
            else if (_eCreated && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && 
                Menu.Item("UseEGap", true).GetValue<bool>()
                && Player.Distance(enemy.Position) > enemy.Distance(_eMissle.Position) && E.IsReady())
            {
                if (_eMissle.EndPosition.Distance(_eMissle.Position) < 400 && 
                    enemy.Distance(_eMissle.Position) < enemy.Distance(_eMissle.EndPosition))
                    E.Cast();
                else if (_eMissle.Position == _eMissle.EndPosition)
                    E.Cast();
            }

        }

        private void GapClose()
        {
            var target = TargetSelector.GetTarget(1500, TargetSelector.DamageType.Magical);
            var distance = Menu.Item("gapD", true).GetValue<Slider>().Value;

            if (!target.IsValidTarget(1500))
                return;

            if (Player.Distance(target.ServerPosition) >= distance &&
                target.IsValidTarget(E.Range) && !_eCreated && 
                E.GetPrediction(target).Hitchance >= HitChance.Medium && E.IsReady())
            {
                E.Cast(target);
            }
        }

        private void LastHit()
        {
            if (!ManaManager.HasMana("LastHit"))
            {
                return;
            }

            var allMinions = MinionManager.GetMinions(Player.ServerPosition, Q.Range);

            if (Q.IsReady() && Menu.Item("UseQLastHit", true).GetValue<bool>())
            {
                foreach (var minion in allMinions)
                {
                    if (minion.IsValidTarget() &&
                        HealthPrediction.GetHealthPrediction
                        (minion, (int)(Player.Distance(minion.Position) * 1000 / 1400)) < 
                        Player.GetSpellDamage(minion, SpellSlot.Q) - 10)
                    {
                        if (Q.IsReady())
                        {
                            Q.Cast(minion);
                            return;
                        }
                    }
                }
            }
        }

        private void Farm()
        {
            if (!ManaManager.HasMana("Farm"))
                return;

            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range + Q.Width);
            var allMinionsW = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, W.Width);
            var allMinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range + E.Width, MinionTypes.All, MinionTeam.NotAlly);

            var useQ = Menu.Item("UseQFarm", true).GetValue<bool>();
            var useW = Menu.Item("UseWFarm", true).GetValue<bool>();
            var useE = Menu.Item("UseEFarm", true).GetValue<bool>();

            if (useE && E.IsReady() && !_eCreated)
            {
                var ePos = E.GetLineFarmLocation(allMinionsE);
                if (ePos.MinionsHit >= 3)
                    E.Cast(ePos.Position);
            }

            if (useQ && Q.IsReady())
            {
                var qPos = Q.GetLineFarmLocation(allMinionsQ);
                if (qPos.MinionsHit >= 2)
                    Q.Cast(qPos.Position);
            }

            if (useW && W.IsReady())
            {
                if (allMinionsW.Count >= 2)
                    W.Cast();
            }
        }
        private void CheckUnderTower()
        {
            foreach (var enemy in HeroManager.Enemies)
            {
                if (Player.Distance(enemy.ServerPosition) <= R.Range)
                {
                    if (ObjectManager.Get<Obj_AI_Turret>()
                        .Where(turret => turret != null && turret.IsValid &&
                        turret.IsAlly && turret.Health > 0)
                        .Any(turret => Vector2.Distance(enemy.Position.To2D(), turret.Position.To2D()) < 750 && 
                        R.IsReady()))
                    {
                        R.Cast(enemy);
                        return;
                    }
                }
            }
        }

        protected override void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead) return;

            DetonateE();

            SmartKs();

            if (Menu.Item("FarmT", true).GetValue<KeyBind>().Active)
                Harass();

            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    if (Menu.Item("UseEGap", true).GetValue<bool>())
                        GapClose();

                    Combo();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass();
                    break;
                case Orbwalking.OrbwalkingMode.LastHit:
                    LastHit();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    Farm();
                    break;
                case Orbwalking.OrbwalkingMode.Freeze:
                    break;
                case Orbwalking.OrbwalkingMode.CustomMode:
                    break;
                case Orbwalking.OrbwalkingMode.None:
                    if (Menu.Item("stunMelles", true).GetValue<KeyBind>().Active && R.IsReady())
                    {
                        foreach (var target in HeroManager.Enemies.Where(x => x.IsValidTarget(200) && !x.HasBuffOfType(BuffType.Invulnerability)).OrderByDescending(GetComboDamage))
                        {
                            R.Cast(target);
                        }
                    }

                    if (Menu.Item("stunTowers", true).GetValue<KeyBind>().Active)
                    {
                        CheckUnderTower();
                    }
                    break;
                case Orbwalking.OrbwalkingMode.Flee:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override void Drawing_OnDraw(EventArgs args)
        {
            foreach (var spell in SpellList)
            {
                var menuItem = Menu.Item(spell.Slot + "Range", true).GetValue<Circle>();
                if (menuItem.Active)
                    Render.Circle.DrawCircle(Player.Position, spell.Slot == SpellSlot.W ? spell.Width : spell.Range, menuItem.Color);
            }

            if (Menu.Item("qExtend", true).GetValue<Circle>().Active)
            {
                Render.Circle.DrawCircle(Player.Position, QExtend.Range, Color.Aquamarine);
            }
        }

        protected override void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!Menu.Item("UseGap", true).GetValue<bool>()) return;

            if (W.IsReady() && gapcloser.Sender.IsValidTarget(W.Width))
                W.Cast();
        }

        protected override void Interrupter_OnPosibleToInterrupt(Obj_AI_Hero unit, Interrupter2.InterruptableTargetEventArgs spell)
        {
            if (!Menu.Item("UseInt", true).GetValue<bool>()) return;

            if (Player.Distance(unit.Position) < R.Range && R.IsReady())
            {
                R.Cast(unit);
            }
        }

        protected override void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (!(sender is MissileClient))
                return;

            var spell = (MissileClient)sender;
            var unit = spell.SpellCaster.Name;
            var name = spell.SData.Name;

            if (unit == ObjectManager.Player.Name && name == "LissandraEMissile")
            {
                _eMissle = spell;
                _eCreated = true;
            }
        }

        protected override void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (!(sender is MissileClient))
                return;

            var spell = (MissileClient)sender;
            var unit = spell.SpellCaster.Name;
            var name = spell.SData.Name;

            if (unit == Player.Name && name == "LissandraEMissile")
            {
                _eMissle = null;
                _eCreated = false;
            }
        }
    }
}
