using SangokuKmy.Models.Common.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Entities
{
    /// <summary>
    /// 認証結果
    /// </summary>
    public class AuthenticationData
    {
        public int Id { get; set; }

        /// <summary>
        /// アクセストークン
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// ユーザのID
        /// </summary>
        public string CharacterId { get; set; }

        /// <summary>
        /// 有効期限
        /// </summary>
        public DateTime ExpirationTime { get; set; }

        /// <summary>
        /// スコープ
        /// </summary>
        public Scope Scope { get; set; }
    }
}
