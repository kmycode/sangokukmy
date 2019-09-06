using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Common;
using SangokuKmy.Models.Services;
using System.Linq;

namespace SangokuKmy.Models.Commands
{
  /// <summary>
  /// 内政コマンド
  /// </summary>
  public abstract class DomesticAffairCommand : Command
  {
    private CommandSystemData Game { get; set; }

    protected IEnumerable<CountryPolicy> Policies { get; private set; }

    protected bool IsStrongStartAvailable => this.Policies.Any(p => p.Type == CountryPolicyType.StrongStart && p.Status == CountryPolicyStatus.Available && p.IntGameDate <= this.Game.GameDateTime.ToInt() && p.IntGameDate + 144 > this.Game.GameDateTime.ToInt());

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      this.Game = game;

      if (this.GetCharacterAssets(character) < this.UseAssetsLength())
      {
        await game.CharacterLogAsync(this.GetCharacterAssetName() + $"が足りません。<num>{this.UseAssetsLength()}</num> 必要です");
        return;
      }

      var townOptional = await repo.Town.GetByIdAsync(character.TownId);
      if (townOptional.HasData)
      {
        var town = townOptional.Data;
        if (town.CountryId != character.CountryId)
        {
          await game.CharacterLogAsync("<town>" + town.Name + "</town>で内政しようとしましたが、自国の都市ではありません");
          return;
        }

        this.Policies = await repo.Country.GetPoliciesAsync(character.CountryId);
        var skills = await repo.Character.GetSkillsAsync(character.Id);

        // 内政値に加算する
        // $znouadd = int(($kint+$kprodmg)/20 + rand(($kint+$kprodmg)) / 40);
        var current = this.GetCurrentValue(town);
        var max = this.GetMaxValue(town);
        var add = (int)(this.GetCharacterAttribute(character) / 20.0f + RandomService.Next(0, this.GetCharacterAttribute(character)) / 40.0f);
        if (add < 1)
        {
          add = 1;
        }
        add = (int)(add * (1 + skills.GetSumOfValues(CharacterSkillEffectType.DomesticAffairMulPercentage) / 100.0f));
        if (current + add >= max)
        {
          this.SetValue(town, max);
        }
        else
        {
          this.SetValue(town, current + add);
        }

        // 経験値、金の増減
        this.SetCharacterAssets(character, this.GetCharacterAssets(character) - this.UseAssetsLength());
        character.Contribution += this.Contributes();
        this.AddCharacterAttributeEx(character, 50);

        await game.CharacterLogAsync("<town>" + town.Name + "</town> の " + this.GetValueName() + " を <num>+" + add + "</num> " + this.GetValueAddingText() + "しました");

        if (RandomService.Next(0, (int)(256.0f * (1 - skills.GetSumOfValues(CharacterSkillEffectType.ItemAppearOnDomesticAffairThousandth) / 1000.0f))) == 0)
        {
          var info = await ItemService.PickTownHiddenItemAsync(repo, character.TownId, character);
          if (info.HasData)
          {
            await game.CharacterLogAsync($"<town>{town.Name}</town> に隠されたアイテム {info.Data.Name} を手に入れました");
          }
        }
      }
      else
      {
        await game.CharacterLogAsync("ID:" + character.TownId + " の都市は存在しません。<emerge>管理者にお問い合わせください</emerge>");
      }
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates);
    }

    protected abstract string GetValueName();
    protected abstract int GetCurrentValue(Town town);
    protected abstract int GetMaxValue(Town town);
    protected abstract void SetValue(Town town, int value);
    protected virtual string GetValueAddingText() => "開発";
    protected virtual int GetCharacterAttribute(Character character) => !this.IsStrongStartAvailable ? character.Intellect : Math.Max(character.Intellect, character.Strong);
    protected virtual void AddCharacterAttributeEx(Character character, short ex)
    {
      if (!this.IsStrongStartAvailable)
      {
        character.AddIntellectEx(ex);
      }
      else
      {
        if (character.Intellect > character.Strong)
        {
          character.AddIntellectEx(ex);
        }
        else
        {
          character.AddStrongEx(ex);
        }
      }
    }
    protected virtual int GetCharacterAssets(Character character) => character.Money;
    protected virtual void SetCharacterAssets(Character character, int value) => character.Money = value;
    protected virtual string GetCharacterAssetName() => "金";
    protected virtual int UseAssetsLength() => 50;
    protected virtual int Contributes() => 30;
  }

  /// <summary>
  /// 農業開発
  /// </summary>
  public class AgricultureCommand : DomesticAffairCommand
  {
    public override CharacterCommandType Type => CharacterCommandType.Agriculture;
    protected override string GetValueName() => "農業";
    protected override int GetCurrentValue(Town town) => town.Agriculture;
    protected override int GetMaxValue(Town town) => town.AgricultureMax;
    protected override void SetValue(Town town, int value) => town.Agriculture = value;
  }

  /// <summary>
  /// 商業発展
  /// </summary>
  public class CommercialCommand : DomesticAffairCommand
  {
    public override CharacterCommandType Type => CharacterCommandType.Commercial;
    protected override string GetValueName() => "商業";
    protected override int GetCurrentValue(Town town) => town.Commercial;
    protected override int GetMaxValue(Town town) => town.CommercialMax;
    protected override void SetValue(Town town, int value) => town.Commercial = value;
  }

  /// <summary>
  /// 技術開発
  /// </summary>
  public class TechnologyCommand : DomesticAffairCommand
  {
    public override CharacterCommandType Type => CharacterCommandType.Technology;
    protected override string GetValueName() => "技術";
    protected override int GetCurrentValue(Town town) => town.Technology;
    protected override int GetMaxValue(Town town) => town.TechnologyMax;
    protected override void SetValue(Town town, int value) => town.Technology = value;
  }

  /// <summary>
  /// 城壁強化
  /// </summary>
  public class WallCommand : DomesticAffairCommand
  {
    public override CharacterCommandType Type => CharacterCommandType.Wall;
    protected override string GetValueName() => "城壁";
    protected override int GetCurrentValue(Town town) => town.Wall;
    protected override int GetMaxValue(Town town) => town.WallMax;
    protected override void SetValue(Town town, int value) => town.Wall = value;
    protected override string GetValueAddingText() => "強化";
  }

  /// <summary>
  /// 米施し
  /// </summary>
  public class SecurityCommand : DomesticAffairCommand
  {
    public override CharacterCommandType Type => CharacterCommandType.Security;
    protected override string GetValueName() => "民忠";
    protected override int GetCurrentValue(Town town) => town.Security;
    protected override int GetMaxValue(Town town) => 100;
    protected override void SetValue(Town town, int value) => town.Security = (short)value;
    protected override string GetValueAddingText() => "回復";
    protected override int GetCharacterAssets(Character character) => character.Rice;
    protected override string GetCharacterAssetName() => "米";
    protected override void SetCharacterAssets(Character character, int value) => character.Rice = value;
    protected override int GetCharacterAttribute(Character character) => !this.IsStrongStartAvailable ? character.Popularity : Math.Max(character.Popularity, character.Strong);
    protected override void AddCharacterAttributeEx(Character character, short ex)
    {
      if (!this.IsStrongStartAvailable)
      {
        character.AddPopularityEx(ex);
      }
      else
      {
        if (character.Intellect > character.Strong)
        {
          character.AddPopularityEx(ex);
        }
        else
        {
          character.AddStrongEx(ex);
        }
      }
    }
  }

  /// <summary>
  /// 緊急米施し
  /// </summary>
  public class SuperSecurityCommand : SecurityCommand
  {
    public override CharacterCommandType Type => CharacterCommandType.SuperSecurity;
    protected override int GetCharacterAttribute(Character character) => character.Popularity * 2;
    protected override int UseAssetsLength() => 200;
    protected override int Contributes() => 60;
  }

  public abstract class DomesticAffairMaxCommand : DomesticAffairCommand
  {
    protected override string GetValueAddingText() => "拡大";
    protected override int GetCharacterAttribute(Character character) => Math.Max(character.Strong, character.Intellect);
    protected override void AddCharacterAttributeEx(Character character, short ex)
    {
      if (character.Intellect > character.Strong)
      {
        character.AddIntellectEx(ex);
      }
      else
      {
        character.AddStrongEx(ex);
      }
    }
  }

  /// <summary>
  /// 農地開拓
  /// </summary>
  public class AgricultureMaxCommand : DomesticAffairMaxCommand
  {
    public override CharacterCommandType Type => CharacterCommandType.AgricultureMax;
    protected override string GetValueName() => "農業最大値";
    protected override int GetCurrentValue(Town town) => town.AgricultureMax;
    protected override int GetMaxValue(Town town) => 3000;
    protected override void SetValue(Town town, int value) => town.AgricultureMax = (short)value;
  }

  /// <summary>
  /// 市場拡大
  /// </summary>
  public class CommercialMaxCommand : DomesticAffairMaxCommand
  {
    public override CharacterCommandType Type => CharacterCommandType.CommercialMax;
    protected override string GetValueName() => "商業最大値";
    protected override int GetCurrentValue(Town town) => town.CommercialMax;
    protected override int GetMaxValue(Town town) => 3000;
    protected override void SetValue(Town town, int value) => town.CommercialMax = (short)value;
  }

  /// <summary>
  /// 城壁増築
  /// </summary>
  public class WallMaxCommand : DomesticAffairMaxCommand
  {
    public override CharacterCommandType Type => CharacterCommandType.WallMax;
    protected override string GetValueName() => "城壁最大値";
    protected override int GetCurrentValue(Town town) => town.WallMax;
    protected override int GetMaxValue(Town town) => 5000;
    protected override void SetValue(Town town, int value) => town.WallMax = (short)value;
  }

  public abstract class BuildingCommand : DomesticAffairCommand
  {
    protected override string GetValueAddingText() => "建築";
    protected override int GetCharacterAttribute(Character character) => Math.Max(character.Strong, character.Intellect);
    protected override void AddCharacterAttributeEx(Character character, short ex)
    {
      if (character.Intellect > character.Strong)
      {
        character.AddIntellectEx(ex);
      }
      else
      {
        character.AddStrongEx(ex);
      }
    }
  }

  /// <summary>
  /// 都市施設
  /// </summary>
  public class TownBuildingCommand : BuildingCommand
  {
    public override CharacterCommandType Type => CharacterCommandType.TownBuilding;
    protected override string GetValueName() => "都市施設";
    protected override int GetCurrentValue(Town town) => town.TownBuildingValue;
    protected override int GetMaxValue(Town town) => Config.TownBuildingMax;
    protected override void SetValue(Town town, int value) => town.TownBuildingValue = value;
  }
}
