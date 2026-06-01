3D Puzzle - Сглобяване на къща
==============================

Описание
--------
Интерактивно 3D приложение за сглобяване на пъзел, разработено в Unity.
Потребителят сглобява проста къща от шест отделни части (под, стени, покрив, врата).
Частите са разпръснати в 3D пространството, а полупрозрачни контури показват
къде трябва да бъде поставена всяка от тях. При приближаване до правилната
позиция парчето автоматично се прилепва на място. Когато всички части са
сглобени, играта приключва с поздравителен екран и конфети ефект.

Технологии
----------
- Unity 2022.3 LTS (3D)
- C# скриптове
- AR Foundation (ARKit за iOS, ARCore за Android)
- Unity UI система за интерфейс
- Процедурно генериране на сцената чрез SceneSetup.cs

Управление (AR)
---------------
- Докосване и плъзгане: хващане и местене на парче върху контура
- Движение на телефона: разглеждане на пъзела от различни ъгли
- Бутон "Reset": започва играта отначало

Стартиране (iOS AR)
-------------------
1. Отворете проекта в Unity 2022.3, превключете платформата на iOS
   (File -> Build Settings -> iOS -> Switch Platform).
2. Edit -> Project Settings -> XR Plug-in Management -> раздел iOS -> ARKit.
3. Build -> отворете генерирания Xcode проект на Mac.
4. Настройте подписване (Signing) с Apple ID и стартирайте на iPhone.
5. Разрешете достъп до камерата при първото стартиране.

Стартиране (Android AR)
-----------------------
1. Отворете проекта в Unity 2022.3, превключете платформата на Android
   (File -> Build Settings -> Android -> Switch Platform).
2. Edit -> Project Settings -> XR Plug-in Management -> раздел Android -> ARCore.
3. Player Settings -> Other Settings: Minimum API Level 24+,
   Scripting Backend = IL2CPP, Target Architectures = ARM64,
   премахнете Vulkan (оставете само OpenGLES3) — изискване на ARCore.
4. Свържете Android устройство с включен USB debugging и натиснете
   Build And Run.
5. Разрешете достъп до камерата при първото стартиране.

Структура на проекта
--------------------
Assets/
  Materials/         - Материали за визуализация (HousePiece, GhostOutline, Ground)
  Scenes/            - Основна сцена (MainScene.unity)
  Scripts/           - C# скриптове:
    SceneSetup.cs       - Процедурно изграждане на сцената + AR rig
    GameManager.cs      - Управление на играта, UI, победа
    PuzzlePiece.cs      - Touch drag-and-drop логика за пъзелните парчета
    ARPuzzlePlacer.cs   - Автоматично поставяне на пъзела пред камерата
    PuzzleScale.cs      - Помощник за мащаба при tabletop размер
    GhostTarget.cs      - Пулсиращ ефект на целевите контури
Packages/            - Unity пакети (manifest.json)
ProjectSettings/     - Настройки на Unity проекта

Автор
-----
Университетски проект, 2026 г.
