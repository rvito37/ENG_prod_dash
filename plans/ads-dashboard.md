---
planStatus:
  planId: plan-ads-dashboard
  title: Дашборд данных ADS
  status: in-progress
  planType: feature
  priority: medium
  owner: AVXUser
  stakeholders: []
  tags:
    - dashboard
    - ads
    - advantage-database
  created: "2026-03-10"
  updated: "2026-03-11T00:00:00.000Z"
  progress: 70
---
# Дашборд данных из Advantage Database Server

## Цель

Лёгкое приложение-дашборд, которое читает данные из ADS (DBF/CDX файлы) и показывает их в современном веб-интерфейсе. Без установки — просто запустил EXE и работаешь.

**Важное требование:** конечный пользователь видит ТОЛЬКО данные. Никаких путей к файлам, названий таблиц, API-эндпоинтов, скриптов — ничего технического.

## Стек технологий

- **Бэкенд:** C# .NET 8, x86, ASP.NET Minimal API (Kestrel)
- **Фронтенд:** одна HTML-страница с Bootstrap 5 + vanilla JS
- **Доступ к ADS:** NuGet `Advantage.Data.Provider` v8.10.1.2
- **Деплой:** self-contained publish → папка с EXE, скопировал и запустил
- **Браузер:** Chrome в --app режиме (1400x750, без адресной строки)

## Подключение к ADS

### LOCAL режим (для разработки)
```
Data Source={путь};ServerType=ADS_LOCAL_SERVER;TableType=ADS_CDX;CharType=OEM;TrimTrailingSpaces=TRUE;
```
- DataPath: `C:\Users\AVXUser\BMS\DATA`
- Нативные DLL: ace32.dll, adsloc32.dll, axcws32.dll + adslocal.cfg, ansi.chr, extend.chr
- Копируются из `C:\Users\AVXUser\PDC-harbour-WINGUI\PdcGui\ADS`

### REMOTE режим (корпоративная машина) ✅ Работает
```
Data Source=\\10.10.48.20\bms\avxbms;ServerType=ADS_REMOTE_SERVER;TableType=ADS_CDX;CharType=OEM;TrimTrailingSpaces=TRUE;
```
- DataPath: `\\10.10.48.20\bms\avxbms` (UNC путь, НЕ маппированный диск G:)
- **Без LockMode** — LockMode=COMPATIBLE вызывает Error 7028 на REMOTE
- Настройки в `appsettings.json`:
```json
{
  "DataPath": "\\\\10.10.48.20\\bms\\avxbms",
  "ServerType": "REMOTE"
}
```

### Деплой на корпоративную машину
1. `dotnet publish -c Release -r win-x86 --self-contained -o ../dash_line_publish`
2. Скопировать папку dash_line_publish на корп. машину
3. Заменить ace32.dll на системный из `C:\Program Files (x86)\Advantage 11.10\ado.net\Redistribute\`
4. Удалить из папки: adsloc32.dll, axcws32.dll, adslocal.cfg
5. Отредактировать appsettings.json (DataPath + ServerType=REMOTE)
6. Запустить DashLine.exe

### Ошибки и решения
| Ошибка | Причина | Решение |
|--------|---------|---------|
| Error 5185 "Local server restricted" | Бандлированный ace32.dll поддерживает только LOCAL | Заменить на системный ace32.dll от Advantage 11.10 |
| Error 7028 "Invalid open mode" | LockMode=COMPATIBLE несовместим с REMOTE | Убрать LockMode из connection string |
| Error 3010 на WHERE по c_btype | CDX index keys с алиасом таблицы | SELECT * без WHERE, кэш в памяти, фильтр в C# |

## Таблицы и данные

### c_pline.dbf — Производственные линии
- Поля: pline_id, pline_nm, pline_pic, ptype_id, size_id, HVal_DT, LVal_DT
- Combo: `PTYPE_ID | PLINE_ID — PLINE_NM [SIZE_ID]` отсортировано по PTYPE+PLINE
- В toolbar: DT Range (HVAL/LVAL) и Format (PLINE_PIC)

### c_btype.dbf — Route Card Description
- Поля: pline_id, esnxx_id, b_type, BTYPE_NME
- Кэш всех 675 строк в памяти, фильтр по pline_id в C#
- Таблица: B_TYPE, ESNXX, Route Card Description (max-width 420px)
- Клик по строке → загрузка ESNXX + EXPQTY + THQTY

### c_esnxx.dbf — ESNXX описания
- Поля: ESNXX_ID, ESNXX_NM, ESNXXTL1-ESNXXTL7
- Панель справа: имя + строки описания (TL1-TL7)

### c_expqty.dbf — Expected Qty
- Поля: B_TYPE, SIZE_ID, LVAL_LIM, HVAL_LIM, TOL_ID, UOM_ID, EXP_YLD
- Фильтр по B_TYPE, сортировка по SIZE_ID + UOM_ID
- Панель справа от ESNXX

### c_thqty.dbf — Threshold Qty
- Поля: B_TYPE, PLINE_ID, SIZE_ID, ORIG_UOM, CONV_UOM, TH_QTY, L_VAL, H_VAL
- Фильтр по B_TYPE + PLINE_ID, сортировка по SIZE_ID + ORIG_UOM
- Панель справа от EXPQTY

## Структура проекта

```
dash_line/
├── DashLine.csproj          # .NET 8, x86, self-contained
├── Program.cs               # Minimal API + Chrome app-mode запуск
├── AdsService.cs            # ADS подключение, кэширование, фильтрация
├── appsettings.json         # DataPath, ServerType
├── wwwroot/
│   └── index.html           # UI дашборда (Bootstrap 5 + vanilla JS)
├── ADS/                     # Нативные DLL (только для LOCAL)
│   ├── ace32.dll
│   ├── adsloc32.dll
│   ├── axcws32.dll
│   ├── adslocal.cfg
│   ├── ansi.chr
│   └── extend.chr
└── plans/
    └── ads-dashboard.md
```

## API эндпоинты

| Метод | URL | Описание |
|-------|-----|----------|
| GET | /api/data/pline | Все производственные линии |
| GET | /api/data/btype/{plineId} | Route card entries по pline |
| GET | /api/data/esnxx/{esnxxId} | ESNXX описание |
| GET | /api/data/expqty/{bType} | Expected qty по B_TYPE |
| GET | /api/data/thqty/{bType}/{plineId} | Threshold qty по B_TYPE + PLINE |

## Будущие доработки

- Добавить другие таблицы по запросу
- Графики/диаграммы для числовых данных
- Фильтры по дате и другим полям
