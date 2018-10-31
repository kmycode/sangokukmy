using SangokuKmy.Models.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Data
{
    /// <summary>
    /// メインとなるリポジトリ
    /// </summary>
    public class MainRepository : IDisposable
    {
        /// <summary>
        /// データベースへアクセスするコンテキスト。アクセスが必要になった場合にのみ生成する
        /// </summary>
        private MainContext Context => this._context = this._context ?? new MainContext();
        private MainContext _context;

        /// <summary>
        /// 認証データ
        /// </summary>
        public AuthenticationDataRepository AuthenticationData => this._auth = this._auth ?? new AuthenticationDataRepository(this.container);
        private AuthenticationDataRepository _auth;

        private readonly IRepositoryContainer container;

        public MainRepository()
        {
            this.container = new Container(this);
        }

        public void Dispose()
        {
            this.Context?.Dispose();
        }

        private class Container : IRepositoryContainer
        {
            private readonly MainRepository repo;

            public MainContext Context => this.repo.Context;

            public Container(MainRepository repo)
            {
                this.repo = repo;
            }
        }
    }

    /// <summary>
    /// リポジトリのコンテナ。リポジトリクラスをnewするときに渡すデータ
    /// </summary>
    public interface IRepositoryContainer
    {
        /// <summary>
        /// データベースへ直接アクセスするコンテキスト
        /// </summary>
        MainContext Context { get; }
    }
}
