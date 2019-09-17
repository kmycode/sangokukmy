using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SangokuKmy.Models.Data.ApiEntities;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("character_commands")]
  public class CharacterCommand
  {
    [Key]
    [Column("id")]
    [JsonIgnore]
    public uint Id { get; set; }

    /// <summary>
    /// 武将ID
    /// </summary>
    [Column("character_id")]
    [JsonProperty("characterId")]
    public uint CharacterId { get; set; }

    /// <summary>
    /// コマンドの種類
    /// </summary>
    [Column("type")]
    [JsonIgnore]
    public CharacterCommandType Type { get; set; }

    /// <summary>
    /// コマンドの種類（JSON出力用）
    /// </summary>
    [NotMapped]
    [JsonProperty("type")]
    public short ApiType
    {
      get => (short)this.Type;
      set => this.Type = (CharacterCommandType)value;
    }

    /// <summary>
    /// コマンドを実行するゲーム内の年月（DB保存用）
    /// </summary>
    [Column("game_date")]
    [JsonIgnore]
    public int IntGameDateTime { get; set; }

    /// <summary>
    /// コマンドを実行するゲーム内の年月
    /// </summary>
    [NotMapped]
    [JsonProperty("gameDate")]
    public GameDateTime GameDateTime
    {
      get => GameDateTime.FromInt(this.IntGameDateTime);
      set => this.IntGameDateTime = value.ToInt();
    }

    /// <summary>
    /// コマンドパラメータ
    /// </summary>
    [NotMapped]
    [JsonProperty("parameters")]
    public IList<CharacterCommandParameter> Parameters { get; private set; } = new List<CharacterCommandParameter>();

    public CharacterCommand SetParameters(IEnumerable<CharacterCommandParameter> parameters)
    {
      if (this.Parameters == null)
      {
        this.Parameters = new List<CharacterCommandParameter>(parameters);
      }
      else
      {
        foreach (var param in parameters)
        {
          this.Parameters.Add(param);
        }
      }
      return this;
    }
  }

  public enum CharacterCommandType : short
  {
    /// <summary>
    /// 何も実行しない
    /// </summary>
    None = 0,

    /// <summary>
    /// 農業開発
    /// </summary>
    Agriculture = 1,

    /// <summary>
    /// 商業発展
    /// </summary>
    Commercial = 2,

    /// <summary>
    /// 技術開発
    /// </summary>
    Technology = 3,

    /// <summary>
    /// 城壁強化
    /// </summary>
    Wall = 4,

    /// <summary>
    /// 守兵増強
    /// </summary>
    [Obsolete]
    WallGuard = 5,

    /// <summary>
    /// 米施し
    /// </summary>
    Security = 6,

    /// <summary>
    /// 農地開拓
    /// </summary>
    AgricultureMax = 7,

    /// <summary>
    /// 市場拡大
    /// </summary>
    CommercialMax = 8,

    /// <summary>
    /// 城壁増築
    /// </summary>
    WallMax = 9,

    /// <summary>
    /// 徴兵
    /// </summary>
    Soldier = 10,

    /// <summary>
    /// 訓練
    /// </summary>
    SoldierTraining = 11,

    /// <summary>
    /// 城の守備
    /// </summary>
    Defend = 12,

    /// <summary>
    /// 戦争
    /// </summary>
    Battle = 13,

    /// <summary>
    /// 集合
    /// </summary>
    Gather = 14,

    /// <summary>
    /// 登用
    /// </summary>
    Promotion = 15,

    /// <summary>
    /// 移動
    /// </summary>
    Move = 17,

    /// <summary>
    /// 能力強化
    /// </summary>
    Training = 18,

    /// <summary>
    /// 米売買
    /// </summary>
    Rice = 19,

    /// <summary>
    /// 仕官
    /// </summary>
    Join = 23,

    /// <summary>
    /// 緊急米施し
    /// </summary>
    SuperSecurity = 30,

    /// <summary>
    /// 都市施設
    /// </summary>
    TownBuilding = 31,

    /// <summary>
    /// 国家施設
    /// </summary>
    [Obsolete]
    CountryBuilding = 32,

    /// <summary>
    /// 研究所
    /// </summary>
    [Obsolete]
    CountryLaboratory = 33,

    /// <summary>
    /// 国庫納入
    /// </summary>
    SafeIn = 34,

    /// <summary>
    /// 国庫搬出
    /// </summary>
    SafeOut = 35,

    /// <summary>
    /// 焼討
    /// </summary>
    [Obsolete]
    Burn = 36,

    /// <summary>
    /// 扇動
    /// </summary>
    [Obsolete]
    Agitation = 37,

    /// <summary>
    /// 兵種研究
    /// </summary>
    ResearchSoldier = 38,

    /// <summary>
    /// 政務官募集
    /// </summary>
    AddSecretary = 39,

    /// <summary>
    /// 政務官配属
    /// </summary>
    Secretary = 40,

    /// <summary>
    /// 政務官削除
    /// </summary>
    RemoveSecretary = 41,

    /// <summary>
    /// 技術破壊
    /// </summary>
    [Obsolete]
    BreakTechnology = 42,

    /// <summary>
    /// 城壁破壊
    /// </summary>
    [Obsolete]
    BreakWall = 43,

    /// <summary>
    /// 政策開発
    /// </summary>
    Policy = 44,

    /// <summary>
    /// 斥候追加
    /// </summary>
    AddScouter = 45,

    /// <summary>
    /// 斥候削除
    /// </summary>
    RemoveScouter = 46,

    /// <summary>
    /// 政務官配属（都市）
    /// </summary>
    SecretaryToTown = 47,

    /// <summary>
    /// 陣形取得
    /// </summary>
    GetFormation = 48,

    /// <summary>
    /// 陣形変更
    /// </summary>
    ChangeFormation = 49,

    /// <summary>
    /// アイテム購入
    /// </summary>
    BuyItem = 50,

    /// <summary>
    /// アイテム売却
    /// </summary>
    SellItem = 51,

    /// <summary>
    /// アイテムを渡す
    /// </summary>
    HandOverItem = 52,

    /// <summary>
    /// 陣形研究
    /// </summary>
    ResearchFormation = 53,

    /// <summary>
    /// 都市巡回
    /// </summary>
    TownPatrol = 54,

    /// <summary>
    /// 都市投資
    /// </summary>
    TownInvest = 55,

    /// <summary>
    /// アイテム使用
    /// </summary>
    UseItem = 56,

    /// <summary>
    /// アイテム生成
    /// </summary>
    GenerateItem = 57,

    /// <summary>
    /// 農民呼寄
    /// </summary>
    PeopleIncrease = 58,

    /// <summary>
    /// 農民避難
    /// </summary>
    PeopleDecrease = 59,

    /// <summary>
    /// 合同訓練
    /// </summary>
    SOldierTrainingAll = 60,

    /// <summary>
    /// 偵察
    /// </summary>
    Spy = 61,
  }
}
