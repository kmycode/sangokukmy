using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Data.Repositories
{
    /// <summary>
    /// 認証データのリポジトリ
    /// </summary>
    public class AuthenticationDataRepository
    {
        private readonly IRepositoryContainer container;

        /// <summary>
        /// データベースのキャッシュ
        /// </summary>
        private IList<AuthenticationData> Cache
        {
            get
            {
                if (_cache == null)
                {
                    this.InitializeCache();
                }
                return _cache;
            }
        }
        private static IList<AuthenticationData> _cache;

        public AuthenticationDataRepository(IRepositoryContainer container)
        {
            this.container = container;
        }

        /// <summary>
        /// データベースのキャッシュを初期化
        /// </summary>
        private void InitializeCache()
        {
            var context = this.container.Context;
            _cache = context.AuthenticationData.ToList();
        }

        /// <summary>
        /// 古いデータを消去する
        /// </summary>
        private void CleanUpOldData()
        {
            var now = DateTime.Now;
            var targets = this.Cache.Where(a => a.ExpirationTime < now).ToList();
            var context = this.container.Context;

            if (!targets.Any())
            {
                return;
            }

            try
            {
                context.AuthenticationData.RemoveRange(targets);
                context.SaveChanges();
                foreach (var target in targets)
                {
                    this.Cache.Remove(target);
                }
            }
            catch
            {
                throw new SangokuKmyException(ErrorCode.DatabaseError);
            }
        }

        /// <summary>
        /// データをトークンから検索する
        /// </summary>
        /// <param name="token">トークン</param>
        /// <returns>認証データ</returns>
        public AuthenticationData FindByToken(string token)
        {
            this.CleanUpOldData();
            return this.Cache.SingleOrDefault(a => a.AccessToken == token);
        }
    }
}
