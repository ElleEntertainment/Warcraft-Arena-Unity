﻿using System.Collections.Generic;
using Common;
using Core.AuraEffects;
using Core.Conditions;
using JetBrains.Annotations;
using UnityEngine;

namespace Core
{
    [UsedImplicitly, CreateAssetMenu(fileName = "Spell Info", menuName = "Game Data/Spells/Spell Info", order = 1)]
    public sealed class SpellInfo : ScriptableUniqueInfo<SpellInfo>
    {
        [SerializeField, UsedImplicitly] private string spellName;
        [SerializeField, UsedImplicitly] private SpellExplicitTargetType explicitTargetType;
        [SerializeField, UsedImplicitly] private SpellDamageClass damageClass;
        [SerializeField, UsedImplicitly] private SpellDispelType spellDispel;
        [SerializeField, UsedImplicitly] private SpellMechanics mechanic;

        [SerializeField, EnumFlag, UsedImplicitly] private SpellCastTargetFlags explicitCastTargets;
        [SerializeField, EnumFlag, UsedImplicitly] private SpellSchoolMask schoolMask;
        [SerializeField, EnumFlag, UsedImplicitly] private SpellPreventionType preventionType;
        [SerializeField, EnumFlag, UsedImplicitly] private SpellAttributes attributes;
        [SerializeField, EnumFlag, UsedImplicitly] private SpellExtraAttributes attributesExtra;
        [SerializeField, EnumFlag, UsedImplicitly] private SpellCustomAttributes attributesCustom;

        [SerializeField, EnumFlag, UsedImplicitly] private EnityTypeMask targetEntityTypeMask;
        [SerializeField, EnumFlag, UsedImplicitly] private SpellRangeFlags rangedFlags;
        [SerializeField, EnumFlag, UsedImplicitly] private SpellInterruptFlags interruptFlags;
        [SerializeField, UsedImplicitly, EnumFlag] private SpellMechanicsFlags castIgnoringMechanics;

        [SerializeField, UsedImplicitly] private int cooldownTime;
        [SerializeField, UsedImplicitly] private int categoryCooldownTime;
        [SerializeField, UsedImplicitly] private int globalCooldownTime;
        [SerializeField, UsedImplicitly] private int castTime;
        [SerializeField, UsedImplicitly] private int minCastTime;

        [SerializeField, UsedImplicitly] private float minRangeHostile;
        [SerializeField, UsedImplicitly] private float minRangeFriend;
        [SerializeField, UsedImplicitly] private float maxRangeHostile;
        [SerializeField, UsedImplicitly] private float maxRangeFriend;
        [SerializeField, UsedImplicitly] private float speed;
        [SerializeField, UsedImplicitly] private int stackAmount;
        [SerializeField, UsedImplicitly] private int maxAffectedTargets;

        [SerializeField, UsedImplicitly] private List<SpellEffectInfo> spellEffectInfos = new List<SpellEffectInfo>();
        [SerializeField, UsedImplicitly] private List<SpellPowerEntry> spellPowerEntries = new List<SpellPowerEntry>();
        [SerializeField, UsedImplicitly] private List<SpellCastCondition> targetingConditions;

        [UsedImplicitly] private SpellMechanicsFlags combinedEffectMechanics;
        [UsedImplicitly] private float maxTargetingRadius;

        /// <summary>
        /// Compressed to 8 bits in <seealso cref="SpellCastRequestEvent"/> and other spell events.
        /// </summary>
        public new int Id => base.Id;
        public string SpellName => spellName;

        public SpellExplicitTargetType ExplicitTargetType => explicitTargetType;
        public SpellCastTargetFlags ExplicitCastTargets => explicitCastTargets;
        public SpellDispelType SpellDispel => spellDispel;
        public SpellMechanics Mechanic => mechanic;
        public SpellMechanicsFlags CastIgnoringMechanics => castIgnoringMechanics;
        public SpellMechanicsFlags CombinedEffectMechanics => combinedEffectMechanics;
        public SpellDamageClass DamageClass => damageClass;

        public SpellSchoolMask SchoolMask => schoolMask;
        public SpellPreventionType PreventionType => preventionType;
        public SpellAttributes Attributes => attributes;
        public SpellExtraAttributes AttributesExtra => attributesExtra;
        public SpellCustomAttributes AttributesCustom => attributesCustom;

        public EnityTypeMask TargetEntityTypeMask => targetEntityTypeMask;
        public SpellRangeFlags RangedFlags => rangedFlags;
        public SpellInterruptFlags InterruptFlags => interruptFlags;

        public List<SpellPowerEntry> PowerCosts => spellPowerEntries;
        public List<SpellEffectInfo> Effects => spellEffectInfos;

        public int CooldownTime => cooldownTime;
        public int CategoryCooldownTime => categoryCooldownTime;
        public int GlobalCooldownTime => globalCooldownTime;
        public int CastTime => castTime;
        public int MinCastTime => minCastTime;

        public float MinRangeHostile => minRangeHostile;
        public float MinRangeFriend => minRangeFriend;
        public float MaxRangeHostile => maxRangeHostile;
        public float MaxRangeFriend => maxRangeFriend;
        public float MaxTargetingRadius => maxTargetingRadius;
        public float Speed => speed;

        public bool IsPassive => HasAttribute(SpellAttributes.Passive);

        public bool IsDeathPersistent => HasAttribute(SpellAttributes.DeathPersistent);

        public bool IsPositive => !HasAttribute(SpellCustomAttributes.Negative);

        public bool IsSingleTarget => HasAttribute(SpellExtraAttributes.SingleTargetSpell);

        public bool IsAffectingArea => Effects.Exists(effect => effect.IsTargetingArea() && effect.IsEffect(SpellEffectType.PersistentAreaAura) || effect.IsAreaAuraEffect());

        public bool IsTargetingArea => Effects.Exists(effect => effect.IsTargetingArea());

        public bool HasEffect(SpellEffectType effectType)
        {
            return Effects.Exists(effect => effect.EffectType == effectType);
        }

        public bool HasAttribute(SpellAttributes attribute)
        {
            return (Attributes & attribute) != 0;
        }

        public bool HasAttribute(SpellExtraAttributes attribute)
        {
            return (AttributesExtra & attribute) != 0;
        }

        public bool HasAttribute(SpellCustomAttributes attribute)
        {
            return (AttributesCustom & attribute) != 0;
        }

        public bool CanPierceImmuneAura(SpellInfo spellInfo)
        {
            // these spells pierce all avalible spells
            if (HasAttribute(SpellAttributes.UnaffectedByInvulnerability))
                return true;

            // these spells can pierce all        
            if (!HasAttribute(SpellAttributes.UnaffectedBySchoolImmune))
                return false;

            // but not these (Divine shield, Ice block, Cyclone and Banish for example)
            return !(spellInfo.Mechanic == SpellMechanics.ImmuneShield || spellInfo.Mechanic == SpellMechanics.Invulnerability || spellInfo.Mechanic == SpellMechanics.Banish);
        }

        public bool CanDispelAura(SpellInfo auraSpellInfo)
        {
            // unaffected by invulnerability spells can dispel any non death persistent aura
            if (HasAttribute(SpellAttributes.UnaffectedByInvulnerability) && !auraSpellInfo.IsDeathPersistent)
                return true;

            // unaffected by invulnerability auras can not be dispelled
            if (auraSpellInfo.HasAttribute(SpellAttributes.UnaffectedByInvulnerability))
                return false;

            // auras ignoring school immunity can not be dispelled
            if (auraSpellInfo.HasAttribute(SpellAttributes.UnaffectedBySchoolImmune))
                return false;

            return true;
        }

        public bool CanCancelAuraType(AuraEffectType auraEffectType, Unit caster)
        {
            IReadOnlyList<AuraEffect> activeEffects = caster.Auras.GetAuraEffects(auraEffectType);
            if (activeEffects == null)
                return true;

            for (int i = 0; i < activeEffects.Count; i++)
            {
                if (CanCancelAura(activeEffects[i]))
                    continue;

                return false;
            }

            return true;
        }

        public bool CanCancelAura(AuraEffect auraEffect)
        {
            if (!HasAttribute(SpellAttributes.DispelAurasOnImmunity))
                return false;

            if (auraEffect.Aura.SpellInfo.HasAttribute(SpellAttributes.UnaffectedByInvulnerability))
                return false;

            foreach (SpellEffectInfo effectInfo in spellEffectInfos)
            {
                if (!(effectInfo is EffectApplyAura applyAuraEffect))
                    continue;

                for (int index = 0; index < applyAuraEffect.AuraInfo.AuraEffects.Count; index++)
                {
                    AuraEffectInfo auraEffectInfo = applyAuraEffect.AuraInfo.AuraEffects[index];
                    switch (auraEffectInfo)
                    {
                        case AuraEffectInfoSchoolImmunity schoolImmunity:
                            if (auraEffect.Aura.SpellInfo.HasAttribute(SpellAttributes.UnaffectedBySchoolImmune))
                                continue;

                            if (schoolImmunity.SchoolMask.HasAnyFlag(SchoolMask))
                                return true;
                            break;
                    }
                }
            }

            return false;
        }

        public SpellCastResult CheckTarget(Unit caster, Unit target, Spell spell, bool isImplicit = true)
        {
            if (HasAttribute(SpellAttributes.CantTargetSelf) && caster == target)
                return SpellCastResult.BadTargets;

            if (!HasAttribute(SpellExtraAttributes.CanTargetInvisible) && !caster.CanSeeOrDetect(target, isImplicit))
                return SpellCastResult.BadTargets;

            if (HasAttribute(SpellCustomAttributes.Pickpocket) && caster is Player && target is Player && caster != target)
                return SpellCastResult.BadTargets;

            if (HasAttribute(SpellAttributes.OnlyTargetPlayers) && !(target is Player))
                return SpellCastResult.TargetNotPlayer;

            if (target != caster && (caster.IsControlledByPlayer || !IsPositive) && target is Player player && !player.IsVisible)
                return SpellCastResult.BmOrInvisGod;

            if (target.HasState(UnitControlState.InFlight) && !HasAttribute(SpellCustomAttributes.AllowInFlightTarget))
                return SpellCastResult.BadTargets;

            if (target.HasAuraType(AuraEffectType.PreventResurrection))
                if (HasEffect(SpellEffectType.SelfResurrect) || HasEffect(SpellEffectType.Resurrect))
                    return SpellCastResult.TargetCannotBeResurrected;

            foreach (SpellCastCondition castCondition in targetingConditions)
                if (castCondition.With(caster, target, spell).IsApplicableAndInvalid)
                    return castCondition.FailedResult;

            return SpellCastResult.Success;
        }

        public SpellCastResult CheckExplicitTarget(Unit caster, Unit target)
        {
            if (ExplicitTargetType != SpellExplicitTargetType.Target)
                return SpellCastResult.Success;

            if (ExplicitCastTargets.HasAnyFlag(SpellCastTargetFlags.UnitMask))
            {
                if(target == null)
                    return SpellCastResult.BadTargets;

                if (ExplicitCastTargets.HasTargetFlag(SpellCastTargetFlags.UnitEnemy) && caster.IsHostileTo(target))
                    return SpellCastResult.Success;

                if (ExplicitCastTargets.HasTargetFlag(SpellCastTargetFlags.UnitAlly) && caster.IsFriendlyTo(target))
                    return SpellCastResult.Success;

                return SpellCastResult.BadTargets;
            }

            return SpellCastResult.Success;
        }

        public float GetMinRange(bool positive)
        {
            return positive ? MinRangeFriend : MinRangeHostile;
        }

        public float GetMaxRange(bool positive, Unit caster = null, Spell spell = null)
        {
            float range = positive ? MaxRangeFriend : MaxRangeHostile;
            if (caster != null && spell != null)
                range = caster.Spells.ApplySpellModifier(spell, SpellModifierType.Range, range);
            return range;
        }

        public List<SpellResourceCost> CalcPowerCost(Unit caster, SpellSchoolMask schoolMask)
        {
            var powers = PowerCosts;
            var costs = new List<SpellResourceCost>(PowerCosts.Count);
            int healthCost = 0;

            foreach (var power in powers)
            {
                // bse powerCost
                int powerCost = power.PowerCost;
                // percent cost from total amount
                if (power.PowerCostPercentage > 0)
                {
                    switch (power.SpellResourceType)
                    {
                        // health as power used
                        case SpellResourceType.Health:
                            powerCost += caster.MaxHealth.CalculatePercentage(power.PowerCostPercentage);
                            break;
                        case SpellResourceType.Mana:
                            powerCost += caster.BaseMana.CalculatePercentage(power.PowerCostPercentage);
                            break;
                        case SpellResourceType.Rage:
                        case SpellResourceType.Focus:
                        case SpellResourceType.Energy:
                            powerCost += caster.GetMaxPower(power.SpellResourceType).CalculatePercentage(power.PowerCostPercentage);
                            break;
                        case SpellResourceType.Runes:
                        case SpellResourceType.RunicPower:
                            Debug.unityLogger.LogWarning("Spells", $"CalculateManaCost for {power.SpellResourceType}: Not implemented yet!");
                            break;
                        default:
                            Debug.unityLogger.LogError("Spells", $"CalculateManaCost: Unknown power type '{power.SpellResourceType}' in spell {Id}");
                            continue;
                    }
                }

                if (power.SpellResourceType == SpellResourceType.Health)
                {
                    healthCost += powerCost;
                    continue;
                }

                bool found = false;
                for (int i = 0; i < costs.Count; i++)
                {
                    if(costs[i].SpellResource == power.SpellResourceType)
                    {
                        costs[i] = new SpellResourceCost(costs[i].SpellResource, costs[i].Amount + powerCost);
                        found = true;
                    }
                }

                if (!found)
                    costs.Add(new SpellResourceCost(power.SpellResourceType, powerCost));
            }

            if (healthCost > 0)
                costs.Add(new SpellResourceCost(SpellResourceType.Health, healthCost));

            costs.RemoveAll(cost => cost.SpellResource != SpellResourceType.Runes && cost.Amount <= 0);

            return costs;
        }

        internal void PopulateEffectInfo()
        {
            combinedEffectMechanics = Mechanic.AsFlag();
            maxTargetingRadius = 0.0f;

            foreach (SpellEffectInfo spellEffectInfo in spellEffectInfos)
            {
                if (spellEffectInfo is EffectApplyAura auraApplyEffect)
                    for (int index = 0; index < auraApplyEffect.AuraInfo.AuraEffects.Count; index++)
                        combinedEffectMechanics |= auraApplyEffect.AuraInfo.AuraEffects[index].Mechanics.AsFlag();

                if (spellEffectInfo.Targeting is SpellTargetingArea areaTargeting)
                    maxTargetingRadius = Mathf.Max(areaTargeting.MaxRadius, maxTargetingRadius);
            }
        }
    }
}