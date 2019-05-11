using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;
using SangokuKmy.Models.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Services
{
  public static class MlService
  {
    public static ILogger Logger { private get; set; }

    public static AiBattleTargetType Predict(IEnumerable<AiBattleHistory> rawData, AiBattleHistory testData)
    {
      var dataCount = rawData.Count();
      if (dataCount == 0)
      {
        return AiBattleTargetType.Unknown;
      }
      if (dataCount == 1)
      {
        return rawData.First().TargetType;
      }

      var firstData = rawData.First();
      if (rawData.All(d => d.TargetType == firstData.TargetType))
      {
        // すべての結果が同じだと例外が出る
        return firstData.TargetType;
      }

      var mlData = rawData
        .Select(d => new AiBattleTargetTypeData
        {
          FloatGameDateTime = d.IntGameDateTime,
          FloatAttackerId = d.CharacterId,
          TargetType = d.TargetType,
        })
        .ToArray();
      var mlTestData = new AiBattleTargetTypeData
      {
        FloatGameDateTime = testData.IntGameDateTime,
        FloatAttackerId = testData.CharacterId,
      };
      
      var context = new MLContext();
      var data = context.Data.LoadFromEnumerable(mlData);

      var pipeline = context.Transforms.Conversion.MapValueToKey("Label", "FloatTargetType")
        .Append(context.Transforms.Concatenate("Features", "FloatGameDateTime", "FloatAttackerId"))
        .AppendCacheCheckpoint(context)
        .Append(context.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features"))
        .Append(context.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

      try
      {
        var model = pipeline.Fit(data);
        var predictionFunction = context.Model.CreatePredictionEngine<AiBattleTargetTypeData, AiBattleTargetTypeResult>(model);
        var prediction = predictionFunction.Predict(mlTestData);

        return prediction.TargetType;
      }
      catch (Exception ex)
      {
        Logger?.LogError(ex, "機械学習で例外が発生しました");
        return AiBattleTargetType.Unknown;
      }
    }

    class AiBattleTargetTypeData
    {
      public float FloatGameDateTime { get; set; }

      public float FloatAttackerId { get; set; }

      [NoColumn]
      public AiBattleTargetType TargetType { get; set; }

      public float FloatTargetType
      {
        get => (short)this.TargetType;
        set => this.TargetType = (AiBattleTargetType)(short)value;
      }
    }

    class AiBattleTargetTypeResult
    {
      [ColumnName("PredictedLabel")]
      public float FloatTargetType { get; set; }

      public AiBattleTargetType TargetType
      {
        get => (AiBattleTargetType)(short)this.FloatTargetType;
      }
    }
  }
}
