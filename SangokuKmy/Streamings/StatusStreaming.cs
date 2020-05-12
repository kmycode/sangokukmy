using System;
using SangokuKmy.Models.Data.Entities.Caches;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Data;
using SangokuKmy.Common;
using SangokuKmy.Models.Common.Definitions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SangokuKmy.Models.Data.ApiEntities;
using System.Collections.Generic;
using System.Linq;

namespace SangokuKmy.Streamings
{
  public class StatusStreaming : CacheStreamingBase<CharacterCache>
  {
    public static StatusStreaming Default => _default = _default ?? new StatusStreaming();
    private static StatusStreaming _default;

    /// <summary>
    /// ストリーミングの対象を追加する
    /// </summary>
    /// <param name="response">レスポンス</param>
    /// <param name="authData">認証データ</param>
    public void Add(HttpResponse response, AuthenticationData authData, Character chara)
    {
      this.Add(response, authData, chara, null);
    }

    public void Add(HttpResponse response, AuthenticationData authData, Character chara, Action onRemoved)
    {
      this.Remove(s => s.ExtraData.Id == chara.Id);
      var data = new StreamingData<CharacterCache>
      {
        AuthData = authData,
        Response = response,
        ExtraData = chara.ToCache(),
      };
      if (onRemoved != null)
      {
        data.Removed += (sender, e) => onRemoved();
      }
      this.Add(data);
    }

    public void Disconnect(Character chara)
    {
      this.Remove(s => s.ExtraData.Id == chara.Id);
    }

    /// <summary>
    /// ストリーミングの条件分岐で使う武将のキャッシュを更新する
    /// </summary>
    /// <param name="charas">更新対象の武将</param>
    public void UpdateCache(IEnumerable<Character> charas)
    {
      this.UpdateUnique(charas.Select(c => c.ToCache()));
    }

    /// <summary>
    /// 現在ストリーミング中のユーザを取得する
    /// </summary>
    /// <returns>ストリーミング中のユーザ一覧</returns>
    public IReadOnlyList<CharacterCache> GetStreamingCharacters() => this.GetList();

    /// <summary>
    /// 全員にデータを送信する
    /// </summary>
    /// <param name="data">送信するデータ</param>
    public async Task SendAllAsync<T>(ApiData<T> data) => await this.SendAsync(data, c => true);

    /// <summary>
    /// 全員にデータを送信する
    /// </summary>
    /// <param name="data">送信するデータ</param>
    public async Task SendAllAsync<T>(IEnumerable<ApiData<T>> data) => await this.SendAsync(data, c => true);

    /// <summary>
    /// 全員に都市データを送信する。その国以外の武将には簡易データのみが送られる
    /// </summary>
    /// <param name="data">都市データ</param>
    public async Task SendTownToAllAsync(ApiData<Town> data, MainRepository repo)
    {
      await this.SendTownToAllAsync(data, repo, data.Data, ApiData.From(new TownForAnonymous(data.Data)));
    }

    /// <summary>
    /// 全員に都市データを送信する。その国以外の武将には簡易データのみが送られる
    /// </summary>
    /// <param name="data">都市データ</param>
    public async Task SendTownToAllAsync<T>(ApiData<T> data, MainRepository repo, Town town)
    {
      await this.SendTownToAllAsync<T, object>(data, repo, town, default);
    }

    /// <summary>
    /// 全員に都市データを送信する。その国以外の武将には簡易データのみが送られる
    /// </summary>
    /// <param name="data">都市データ</param>
    public async Task SendTownToAllAsync<T, U>(ApiData<T> data, MainRepository repo, Town town, ApiData<U> anonymousData)
    {
      var townCharacters = (await repo.Town.GetCharactersAsync(town.Id)).Where(c => c.CountryId != town.CountryId);
      await this.SendAsync(data, c => c.ExtraData.CountryId == town.CountryId || townCharacters.Any(tc => tc.Id == c.ExtraData.Id));
      if (anonymousData != null)
      {
        await this.SendAsync(anonymousData, c => c.ExtraData.CountryId != town.CountryId && !townCharacters.Any(tc => tc.Id == c.ExtraData.Id));
      }
    }

    /// <summary>
    /// 特定のIDを持った武将にのみ送信する
    /// </summary>
    /// <param name="data">送信するデータ</param>
    /// <param name="charaId">武将ID</param>
    public async Task SendCharacterAsync<T>(ApiData<T> data, uint charaId) => await this.SendAsync(data, c => c.ExtraData.Id == charaId);

    /// <summary>
    /// 特定のIDを持った武将にのみ送信する
    /// </summary>
    /// <param name="data">送信するデータ</param>
    /// <param name="charaIds">武将IDの一覧</param>
    public async Task SendCharacterAsync<T>(ApiData<T> data, IEnumerable<uint> charaIds) => await this.SendAsync(data, c => charaIds.Contains(c.ExtraData.Id));

    /// <summary>
    /// 特定のIDを持った武将にのみ送信する
    /// </summary>
    /// <param name="data">送信するデータ</param>
    /// <param name="charaId">武将ID</param>
    public async Task SendCharacterAsync(IEnumerable<IApiData> data, uint charaId) => await this.SendAsync(data, c => c.ExtraData.Id == charaId);

    /// <summary>
    /// 特定のIDを持った武将にのみ送信する
    /// </summary>
    /// <param name="data">送信するデータ</param>
    /// <param name="charaIds">武将IDの一覧</param>
    public async Task SendCharacterAsync(IEnumerable<IApiData> data, IEnumerable<uint> charaIds) => await this.SendAsync(data, c => charaIds.Contains(c.ExtraData.Id));

    /// <summary>
    /// 特定のIDを持った武将以外の、同じ国の武将に送信する
    /// </summary>
    /// <param name="data">送信するデータ</param>
    public async Task SendCountryExceptForCharacterAsync<T>(ApiData<T> data, uint countryId, uint charaId) => await this.SendAsync(data, c => c.ExtraData.Id != charaId && c.ExtraData.CountryId == countryId);

    /// <summary>
    /// 特定のIDを持った武将以外の、同じ国の武将に送信する
    /// </summary>
    /// <param name="data">送信するデータ</param>
    public async Task SendCountryExceptForCharacterAsync<T>(ApiData<T> data, uint countryId, IEnumerable<uint> charaIds) => await this.SendAsync(data, c => !charaIds.Contains(c.ExtraData.Id) && c.ExtraData.CountryId == countryId);

    /// <summary>
    /// 特定のIDを持った武将とその国以外の武将に送信する
    /// </summary>
    /// <param name="data">送信するデータ</param>
    public async Task SendAllExceptForCountryAsync<T>(ApiData<T> data, uint countryId) => await this.SendAsync(data, c => c.ExtraData.CountryId != countryId);

    /// <summary>
    /// 特定のIDを持った武将以外の武将に送信する
    /// </summary>
    /// <param name="data">送信するデータ</param>
    public async Task SendExceptForCharactersAsync<T>(ApiData<T> data, IEnumerable<uint> charaIds) => await this.SendAsync(data, c => !charaIds.Contains(c.ExtraData.Id));

    /// <summary>
    /// 特定のIDを持った武将以外の武将に送信する
    /// </summary>
    /// <param name="data">送信するデータ</param>
    public async Task SendCharacterExceptForCharactersAndCountryAsync<T>(ApiData<T> data, IEnumerable<uint> charaIds, uint countryId) => await this.SendAsync(data, c => !charaIds.Contains(c.ExtraData.Id) && c.ExtraData.CountryId != countryId);

    /// <summary>
    /// 特定のIDを持った武将以外の武将に送信する
    /// </summary>
    /// <param name="data">送信するデータ</param>
    public async Task SendCharacterExceptForCharactersAndCountryAsync<T>(ApiData<T> data, IEnumerable<uint> charaIds, IEnumerable<uint> countryIds) => await this.SendAsync(data, c => !charaIds.Contains(c.ExtraData.Id) && !countryIds.Contains(c.ExtraData.CountryId));

    /// <summary>
    /// 特定の国の武将にのみ送信する
    /// </summary>
    /// <param name="data">送信するデータ</param>
    /// <param name="countryId">国ID</param>
    public async Task SendCountryAsync<T>(ApiData<T> data, uint countryId) => await this.SendAsync(data, c => c.ExtraData.CountryId == countryId);

    /// <summary>
    /// 特定の国の武将にのみ送信する
    /// </summary>
    /// <param name="data">送信するデータ</param>
    /// <param name="countryId">国ID</param>
    public async Task SendCountryAsync<T>(ApiData<T> data, IEnumerable<uint> countryIds) => await this.SendAsync(data, c => countryIds.Contains(c.ExtraData.CountryId));

    /// <summary>
    /// 特定の国の武将にのみ送信する
    /// </summary>
    /// <param name="data">送信するデータ</param>
    /// <param name="countryId">国ID</param>
    public async Task SendCountryAsync(IEnumerable<IApiData> data, uint countryId) => await this.SendAsync(data, c => c.ExtraData.CountryId == countryId);
  }
}
