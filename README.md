# 三国志NET KMY Version 9

ASP.NET Core MVCとVue.jsで来年４月までに実装したける！！！！！！（フラグ）

三国志NET KMY Versionは、[https://sangoku.kmycode.net/](https://sangoku.kmycode.net/) で稼働中です。興味のある方は参加してみてくださいなのですー。

これはAPIサーバの開発リポジトリですが、別に[クライアントアプリ（HTML+SCSS+TypeScript+Vue.js+axios+Bootstrap）のリポジトリ](https://github.com/kmycode/sangokukmy-client)があります。あわせてご利用くださいまし。

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
