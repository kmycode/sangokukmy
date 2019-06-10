﻿using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Streamings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Services
{
  public static class SkillService
  {
    public static async Task SetCharacterAsync(MainRepository repo, CharacterSkill item, Character chara)
    {
      var strong = (short)item.GetSumOfValues(CharacterSkillEffectType.Strong);
      var intellect = (short)item.GetSumOfValues(CharacterSkillEffectType.Intellect);
      var leadership = (short)item.GetSumOfValues(CharacterSkillEffectType.Leadership);
      var popularity = (short)item.GetSumOfValues(CharacterSkillEffectType.Popularity);

      chara.Strong += strong;
      chara.Intellect += intellect;
      chara.Leadership += leadership;
      chara.Popularity += popularity;

      item.CharacterId = chara.Id;
      item.Status = CharacterSkillStatus.Available;

      await StatusStreaming.Default.SendAllAsync(ApiData.From(chara));
      await StatusStreaming.Default.SendAllAsync(ApiData.From(item));
    }

    public static async Task ReleaseCharacterAsync(MainRepository repo, CharacterSkill item, Character chara)
    {
      var strong = (short)item.GetSumOfValues(CharacterSkillEffectType.Strong);
      var intellect = (short)item.GetSumOfValues(CharacterSkillEffectType.Intellect);
      var leadership = (short)item.GetSumOfValues(CharacterSkillEffectType.Leadership);
      var popularity = (short)item.GetSumOfValues(CharacterSkillEffectType.Popularity);

      chara.Strong -= strong;
      chara.Intellect -= intellect;
      chara.Leadership -= leadership;
      chara.Popularity -= popularity;

      item.Status = CharacterSkillStatus.Undefined;

      await StatusStreaming.Default.SendAllAsync(ApiData.From(chara));
      await StatusStreaming.Default.SendAllAsync(ApiData.From(item));
    }
  }
}
