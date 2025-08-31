# AddressableSupportLib

Unity Addressablesを効率的・柔軟に管理/活用するためのC#ライブラリです。  
アドレッサブルアセットのロード・管理・解放を抽象化し、プロジェクトの規模や用途に応じた柔軟なリソース管理をサポートします。

---

## 特長

- **アドレッサブルアセットの型安全なロード/管理**
- **グループ・カテゴリ単位での整理・検索・一括解放**
- **シーンリンクや自動解放(AutoRelease)の仕組み**
- **シンプルなAPIとシングルトンによる統合管理**

---

## 主な構成

- `BaseAddressableData.cs`  
  アドレッサブルデータの抽象基底クラス。ロード状態や自動解放フラグなど共通管理を提供。

- `AddressableData.cs`  
  型付き(T)アドレッサブルデータの実装。  
  単体/配列ロード、インスタンス生成、解放(Release)処理を提供。

- `AddressableObject.cs`  
  アドレッサブルアセットの汎用ラッパー。  
  非同期ロード、インスタンス化、リリースを簡潔に呼び出し可能。

- `AddressableDataContainer.cs`  
  グループ・カテゴリでデータを管理するコンテナ。  
  検索・追加・一括解放・統計情報などを提供。

- `AddressableDataCore.cs`  
  ライブラリの中核となるシングルトンクラス。  
  AddressableDataContainerによる一元管理、シーンごとの管理、  
  自動解放ルーチン、各種ファクトリメソッドなどを実装。

---

## 使い方

### 1. AddressableObjectで単体ロード

```csharp
var addressable = AddressableDataCore.CreateAddressable<GameObject>("Assets/Prefabs/MyPrefab.prefab");
await addressable.LoadAsync(obj => {
    // obj: ロードされたGameObject
});
```

### 2. AddressableDataを使った拡張管理

```csharp
var data = new AddressableData<GameObject>(
    GroupCategory.Game,
    AssetCategory.Prefab
);
await data.LoadAsync("Assets/Prefabs/MyPrefab.prefab", obj => {
    // obj 利用
});

// インスタンス化
var instance = data.Instantiate("MyInstance");
```

### 3. グループ/カテゴリ単位で一括解放

```csharp
AddressableDataCore.Instance.ReleaseGroup(GroupCategory.Game);
AddressableDataCore.Instance.ReleaseCategory(GroupCategory.Game, AssetCategory.Prefab);
```

### 4. 自動解放(AutoRelease)の利用

```csharp
data.EnableAutoRelease();
// 未使用になったら自動解放ルーチンでメモリ開放
```

---

## 列挙体(Enums)

- **AssetCategory**  
  - Prefab / Texture / Audio / UI / Other
- **GroupCategory**  
  - Title / Game / Exit / Menu / Other

---

## 依存

- Unity (Addressables, ResourceManagement)
- Cysharp UniTask

---

## 注意事項

- Addressablesシステムのセットアップは別途必要です  
- 各APIの詳細挙動はコードコメントを参照してください

---

## ライセンス

MIT License

---

## 作者

[ChigaDio](https://github.com/ChigaDio)