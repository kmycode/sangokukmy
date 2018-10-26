# 三国志NET KMY Version 9

ASP.NET Core MVCとVue.jsで来年４月までに実装したける！！！！！！（フラグ）

2018年中は多分そんなに進めません。2019年2～3月が主な開発時期になるかと。
また、これはAPIサーバの開発リポジトリですが、別にクライアントアプリ（HTML+SCSS+JavaScript+Vue.js+axios+Bootstrap）のリポジトリを立ち上げる予定です。後日

API一覧はWikiへ。

# 著作権について

本ゲームプログラムは、「三国志NET」という名前を冠しています。
技術的には私が一から全部作ったものですが、体裁としてはmaccyu氏の制作されたPerlで記述されたスクリプトを改造したもの、という認識でいかせていただこうと思います。

BB氏の作成された画像ファイルは再頒布不可ということで本リポジトリに置く予定はありません。必要があれば適当なとこから落としてください。

連絡などはツイッター（[@askyq](https://twitter.com/askyq)）へ。[三国志NET 翼](http://ysks.sakura.ne.jp/tubasa/index.cgi)にもいるかもしれません。

# ビルド手順
## 画像について

画像は、[三国志NET原作](https://github.com/runtBlue/sangokushi-NET.original)の `image` フォルダの中にあります。

このフォルダの中身のうち、以下のものをコピーします。

~~~
1.gif
2.gif
3.gif

 ...

98.gif
wiz.gif
~~~

コピー先は `/SangokuKmy/wwwroot/images/character-default-icons` フォルダ内です。
