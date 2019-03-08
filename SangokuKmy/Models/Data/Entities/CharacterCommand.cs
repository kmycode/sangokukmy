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
    CountryBuilding = 32,

    /// <summary>
    /// 研究所
    /// </summary>
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
    Burn = 36,

    /// <summary>
    /// 扇動
    /// </summary>
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
  }
}
