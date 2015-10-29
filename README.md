##CM3D2.IKCMOController.Plugin

メイドエディット画面・デイリー画面・夜伽画面中にシステムメニューの  
「IKCMO Controller」ボタン押下で有効化。  ハンドルを操作する事でメイドのポーズの変更が可能。  
![GUI](http://i.imgur.com/9GNg81s.png  "説明")  



##導入方法

**前提条件** : **UnityInjector** が導入済みであること。  
  
[![ダウンロードボタン][img_download]][master zip]を押してzipファイルをダウンロード。  
zipファイルの中にあるUnityInjectorフォルダをCM3D2フォルダにD&Dすれば導入完了。  



##更新履歴

###0.0.2.3
* 上半身角度のIKハンドルを追加。
* 各IKハンドルマウスオーバー時にスクロールで捻り回転を出来るように変更。
 * ハンドルをドラッグすると回転は初期化されます。位置を決めてから捻って下さい。
* IK初期化ボタンをESCキーからマウスホイールクリックに変更。
* 頭部ハンドルの初期位置をポーズ角度に沿うように修正。
* 有効化時にメニューボタンの文字色が変わっていなかったバグ修正。


###0.0.1.2
* ポジションハンドル（以下PH）追加。
 * PHドラッグでメイドの位置を操作可能。
 * PHマウスオーバー時、スクロールでカメラ平面X軸に対し回転。
 * PHマウスオーバー時、左ALT+スクロールでカメラ平面Z軸に対し回転。
 * PHマウスオーバー時、左CTRT+スクロールでメイドY軸に対し回転。
 * 回転操作時に左SHIFT押下で10倍速回転。
 * PHマウスオーバー時、ESCキー押下でIK操作を初期化。（位置は初期化されません）
* デイリー画面（執務室）・夜伽画面でも有効化できるように変更。
* 複数のメイドも操作できるように変更。
* 頭のハンドルが首に隠れないよう修正。


#####0.0.0.1
* [GearMenu][]を利用してメニューにボタン追加するように修正。

###0.0.0.0
* 初版



##注意書き

個人で楽しむ為の非公式Modです。  
転載・再配布・改変・改変物配布等はKISSに迷惑のかからぬ様、  
各自の判断・責任の下で行って下さい。  



[GearMenu]: https://github.com/neguse11/cm3d2_plugins_okiba/blob/master/Lib/GearMenu.cs "GearMenu.cs"
[master zip]:https://github.com/CM3D2-01/CM3D2.IKCMOController.Plugin/archive/master.zip "master zip"
[img_download]: http://i.imgur.com/byav3Uf.png "ダウンロードボタン"
