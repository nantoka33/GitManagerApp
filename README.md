# Git操作ツール / Git Manager App

Git操作をよりスムーズに。WPFで開発されたGUIアプリケーションで、直感的な操作と自動化を実現します。

Make Git operations smoother and more intuitive. This WPF-based GUI application helps you automate and manage common Git tasks.

---

## 🧰 機能 / Features

- ✅ プロジェクト名の履歴選択 / Project name history and reuse
- ✅ Git初期化＆初回Push / Git init + initial push
- ✅ 通常のPull / Standard pull from remote
- ✅ PR対応のPull+Push（任意ブランチ・メッセージ指定可） / Pull + Push for PR (custom branch/message)
- ✅ 実行スケジュール予約機能（日時指定） / Scheduled pull+push execution
- ✅ 未実行予約の表示・削除 / View and remove unexecuted jobs
- ✅ ダークテーマ対応 / Dark theme ready

---

## 🖥️ スクリーンショット

![image](https://github.com/user-attachments/assets/d78bdf69-7eee-435d-98fb-0d5a8ca5cd63)


---

## 📁 フォルダ構成 / Folder Structure

```
GitManagerApp/
├── .gitignore
├── GitManagerApp.sln
├── LICENSE
├── README.md
├── GitManagerApp/
│   ├── App.xaml
│   ├── App.xaml.cs
│   ├── GitExecutor.cs
│   ├── GitManagerApp.csproj
│   ├── MainWindow.xaml
│   ├── MainWindow.xaml.cs
│   ├── Resources/
│   │   ├── DarkTheme.xaml
│   │   └── LightTheme.xaml
│   ├── Services/
│   │   ├── AppDataService.cs
│   │   ├── DropHandler.cs
│   │   ├── GroupRenderer.cs
│   │   ├── IconLoader.cs
│   │   └── ThemeSwitcher.cs
│   └── app_launcher_icon.ico
```
