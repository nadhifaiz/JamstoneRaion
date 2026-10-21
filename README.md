# JamstoneRaion

Game 2D buatan Unity dengan siklus harian selama 30 hari. Tiap hari pemain melewati tiga fase berurutan, masing-masing hanya bisa dibuka sekali per hari lewat Hub: **Collect → Craft → Sell**.

## Alur Game

| Fase | Ringkasan |
|------|-----------|
| **Collect** | Mengendalikan capit (crane) untuk mengambil rongsokan yang bertumpuk dalam 3 layer dan memasukkannya ke bin. Fase berakhir saat baterai habis atau di-skip. Tier material yang muncul bergantung pada hari (kurva probabilitas di `MaterialSpawner`). |
| **Craft** | Menaruh 2 material di meja rakit (drag-and-drop atau klik), lalu merakit. Hasil ditentukan oleh kombinasi tag material. Resep yang salah menghanguskan kedua bahan. Hasil ditampilkan lewat layar overview. |
| **Sell** | Menaruh barang jadi di etalase (3 slot). Pelanggan datang satu per satu, memilih barang secara acak, dan membeli kalau ada di etalase. Fase berjalan dengan timer. |

### Ekonomi
- Harga jual barang = `basePrice` barang + harga bahan 1 + harga bahan 2.
- Harga bahan = harga dasar tag (tabel di `CraftingManager`) x pengali tier (Tier 1 = 100%, Tier 2 = 120%, Tier 3 = 140%).
- Harga disimpan per unit barang di inventory, bukan di ScriptableObject.

## Struktur Kode

```
Assets/Script/
├── Collect/     Capit, spawner rongsokan, bin, baterai
├── Craft/       Resep, meja rakit, UI crafting
├── Inventory/   Penyimpanan bahan & barang jadi, panel inventory
├── Sell/        Etalase, pelanggan, SellManager, mata uang (Amber)
├── Manager/     GameManager (state machine Hub/Collect/Craft/Sell/DayEnd)
├── SO/          ScriptableObject data (MaterialSO, CraftedItemSO)
└── UI/          Helper UI yang dipakai bersama
```

## Menjalankan

1. Buka project dengan **Unity 6000.5.10f1** (lihat `ProjectSettings/ProjectVersion.txt`).
2. Buka scene `Assets/Scenes/TestingScene.unity`.
3. Pastikan layer `Material0`, `Material1`, `Material2`, dan `Bin` ada di *Project Settings > Tags and Layers*.
4. Tekan Play.

## Catatan Pengembangan

- Log debug memakai `GameLog.Log(...)` yang otomatis hilang dari build rilis.
- Urutan enum `MaterialType` jangan diubah, karena tersimpan sebagai angka di aset.