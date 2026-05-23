using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NoteBookUI : MonoBehaviour
{
    private enum NoteBookSection
    {
        Home,
        Intelligence,
        Enemy
    }

    [Header("Canvas Group")]
    private CanvasGroup canvasGroup;

    [Header("Main Page Roots")]
    [SerializeField] private Transform leftPageRoot;
    [SerializeField] private Transform rightPageRoot;

    [Header("Turning Page Roots")]
    [SerializeField] private RectTransform turningPageLeftRoot;
    [SerializeField] private RectTransform turningPageRightRoot;

    [Header("Home Page Contents")]
    [SerializeField] private GameObject leftHomePageContent;
    [SerializeField] private GameObject rightHomePageContent;

    [Header("Home Page Contents For Turning")]
    [SerializeField] private GameObject turningLeftHomePageContent;
    [SerializeField] private GameObject turningRightHomePageContent;

    [Header("Page Prefabs")]
    [SerializeField] private EnemyPageUI enemyPagePrefab;
    [SerializeField] private IntelligencePageUI intelligencePagePrefab;

    [Header("Navigation Buttons")]
    [SerializeField] private GameObject bookMarkButtonDecoration;
    [SerializeField] private Button bookMarkButton;
    [SerializeField] private Button lastPageButton;
    [SerializeField] private Button nextPageButton;

    [Header("Unknown Enemy Settings")]
    [SerializeField] private Sprite unknownEnemySprite;
    [SerializeField] private string unknownEnemyName = "???";
    [SerializeField] private string unknownEnemyInformationLine = "???";
    [SerializeField] private int unknownEnemyInformationLineCount = 3;

    [Header("Page Settings")]
    [SerializeField] private int intelligencesPerPage = 4;

    [Tooltip("实例化出来的页是否自动铺满父物体。通常建议开启。")]
    [SerializeField] private bool stretchInstantiatedPagesToParent = true;

    [Header("Page Turn Animation")]
    [SerializeField] private float halfTurnDuration = 0.2f;
    [SerializeField] private float bookMarkTurnInterval = 0.03f;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private AnimationCurve pageTurnCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Locked Enemy Picture Settings")]
    [SerializeField] private Sprite lockedEnemyPictureSprite;

    [Header("Auto Focus Settings")]
    [SerializeField] private float autoFocusTurnInterval = 0.05f;

    private NoteBookSection currentSection = NoteBookSection.Home;
    private int currentSpreadIndex = 0;

    private int enemySpreadCount = 1;
    private int intelligenceSpreadCount = 1;

    private bool isTurning = false;
    public bool IsBusy => isTurning;

    private Coroutine pageTurnCoroutine;

    private readonly List<GameObject> createdRuntimePageObjects = new List<GameObject>();

    private readonly List<GameObject> leftPageContents = new List<GameObject>();
    private readonly List<GameObject> rightPageContents = new List<GameObject>();
    private readonly List<GameObject> turningLeftPageContents = new List<GameObject>();
    private readonly List<GameObject> turningRightPageContents = new List<GameObject>();

    private readonly List<EnemyPageUI> leftEnemyPages = new List<EnemyPageUI>();
    private readonly List<EnemyPageUI> rightEnemyPages = new List<EnemyPageUI>();
    private readonly List<EnemyPageUI> turningLeftEnemyPages = new List<EnemyPageUI>();
    private readonly List<EnemyPageUI> turningRightEnemyPages = new List<EnemyPageUI>();

    private readonly List<IntelligencePageUI> leftIntelligencePages = new List<IntelligencePageUI>();
    private readonly List<IntelligencePageUI> rightIntelligencePages = new List<IntelligencePageUI>();
    private readonly List<IntelligencePageUI> turningLeftIntelligencePages = new List<IntelligencePageUI>();
    private readonly List<IntelligencePageUI> turningRightIntelligencePages = new List<IntelligencePageUI>();

    private readonly List<EnemyInformationDataSO> currentUnlockedEnemies = new List<EnemyInformationDataSO>();
    private readonly List<IntelligenceDataSO> currentUnlockedIntelligences = new List<IntelligenceDataSO>();

    private InGameUI inGameUI;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        inGameUI = GetComponentInParent<InGameUI>();

        HideTurningPagesImmediate();
    }

    public void Open()
    {
        gameObject.SetActive(true);

        StopCurrentTurnCoroutine();

        currentSection = NoteBookSection.Home;
        currentSpreadIndex = 0;
        isTurning = false;

        RebuildAllPagesFromArchive();

        ShowHomePagesImmediate();
        HideTurningPagesImmediate();

        SetInputEnabled(true);
        RefreshNavigationButtons();
    }

    public void Close()
    {
        StopCurrentTurnCoroutine();

        isTurning = false;

        HideTurningPagesImmediate();
        SetInputEnabled(true);

        gameObject.SetActive(false);
    }

    public void OnClickIntelligenceOption()
    {
        if (currentSection != NoteBookSection.Home)
        {
            return;
        }

        // 从初始页翻到内容页时，装饰书签不能一开始就隐藏
        // 它要等 TurningPageLeft 盖下来的动画结束后，再被真正的 BookMarkButton 替换
        TryStartPageTurn(FlipRightToSectionRoutine(NoteBookSection.Intelligence), true);
    }

    public void OnClickEnemyOption()
    {
        if (currentSection != NoteBookSection.Home)
        {
            return;
        }

        // 从初始页翻到内容页时，装饰书签不能一开始就隐藏
        // 它要等 TurningPageLeft 盖下来的动画结束后，再被真正的 BookMarkButton 替换
        TryStartPageTurn(FlipRightToSectionRoutine(NoteBookSection.Enemy), true);
    }

    public void OnClickNextPage()
    {
        if (currentSection == NoteBookSection.Home)
        {
            return;
        }

        int spreadCount = GetCurrentSectionSpreadCount();

        if (currentSpreadIndex >= spreadCount - 1)
        {
            return;
        }

        TryStartPageTurn(FlipRightToNextSpreadRoutine());
    }

    public void OnClickLastPage()
    {
        if (currentSection == NoteBookSection.Home)
        {
            return;
        }

        TryStartPageTurn(FlipLeftToPreviousSpreadRoutine());
    }

    public void OnClickBookMark()
    {
        if (currentSection == NoteBookSection.Home)
        {
            return;
        }

        TryStartPageTurn(FlipLeftToHomeByBookMarkRoutine());
    }

    public void OnClickExit()
    {
        inGameUI.ToggleNoteBookUI();
    }

    public void OpenToUnlockedArchiveEntry(ArchiveUnlockRecord unlockRecord)
    {
        if (unlockRecord == null)
        {
            Open();
            return;
        }

        gameObject.SetActive(true);

        StopCurrentTurnCoroutine();

        currentSection = NoteBookSection.Home;
        currentSpreadIndex = 0;
        isTurning = false;

        RebuildAllPagesFromArchive();

        ShowHomePagesImmediate();
        HideTurningPagesImmediate();

        pageTurnCoroutine = StartCoroutine(AutoOpenToUnlockedEntryRoutine(unlockRecord));
    }

    private void RebuildAllPagesFromArchive()
    {
        ClearRuntimePages();
        ResetPageContentCollections();

        currentUnlockedEnemies.Clear();
        currentUnlockedIntelligences.Clear();

        IntelligenceArchiveManager archiveManager = IntelligenceArchiveManager.Instance;

        if (archiveManager != null)
        {
            currentUnlockedEnemies.AddRange(archiveManager.GetUnlockedEnemies());
            currentUnlockedIntelligences.AddRange(archiveManager.GetUnlockedIntelligences());
        }
        else
        {
            Debug.LogWarning("NoteBookUI 无法读取图鉴数据：场景中没有 IntelligenceArchiveManager。");
        }

        BuildEnemyPages(currentUnlockedEnemies, archiveManager);
        BuildIntelligencePages(currentUnlockedIntelligences);
    }

    private void ClearRuntimePages()
    {
        for (int i = 0; i < createdRuntimePageObjects.Count; i++)
        {
            if (createdRuntimePageObjects[i] != null)
            {
                Destroy(createdRuntimePageObjects[i]);
            }
        }

        createdRuntimePageObjects.Clear();

        leftEnemyPages.Clear();
        rightEnemyPages.Clear();
        turningLeftEnemyPages.Clear();
        turningRightEnemyPages.Clear();

        leftIntelligencePages.Clear();
        rightIntelligencePages.Clear();
        turningLeftIntelligencePages.Clear();
        turningRightIntelligencePages.Clear();
    }

    private void ResetPageContentCollections()
    {
        leftPageContents.Clear();
        rightPageContents.Clear();
        turningLeftPageContents.Clear();
        turningRightPageContents.Clear();

        AddContentIfNotNull(leftPageContents, leftHomePageContent);
        AddContentIfNotNull(rightPageContents, rightHomePageContent);
        AddContentIfNotNull(turningLeftPageContents, turningLeftHomePageContent);
        AddContentIfNotNull(turningRightPageContents, turningRightHomePageContent);
    }

    private void AddContentIfNotNull(List<GameObject> targetList, GameObject content)
    {
        if (content != null)
        {
            targetList.Add(content);
        }
    }

    private void BuildEnemyPages(List<EnemyInformationDataSO> unlockedEnemies, IntelligenceArchiveManager archiveManager)
    {
        if (enemyPagePrefab == null)
        {
            Debug.LogWarning("NoteBookUI 生成怪物页失败：enemyPagePrefab 没有赋值。");
            enemySpreadCount = 1;
            return;
        }

        int enemyCount = 0;

        if (unlockedEnemies != null)
        {
            enemyCount = unlockedEnemies.Count;
        }

        enemySpreadCount = Mathf.Max(1, Mathf.CeilToInt(enemyCount / 2f));

        for (int spreadIndex = 0; spreadIndex < enemySpreadCount; spreadIndex++)
        {
            int leftEnemyIndex = spreadIndex * 2;
            int rightEnemyIndex = spreadIndex * 2 + 1;

            EnemyInformationDataSO leftEnemyData = GetEnemyDataByIndex(unlockedEnemies, leftEnemyIndex);
            EnemyInformationDataSO rightEnemyData = GetEnemyDataByIndex(unlockedEnemies, rightEnemyIndex);

            EnemyPageUI leftPage = CreatePageInstance(enemyPagePrefab, leftPageRoot, leftEnemyPages, leftPageContents);
            EnemyPageUI rightPage = CreatePageInstance(enemyPagePrefab, rightPageRoot, rightEnemyPages, rightPageContents);
            EnemyPageUI turningLeftPage = CreatePageInstance(enemyPagePrefab, turningPageLeftRoot, turningLeftEnemyPages, turningLeftPageContents);
            EnemyPageUI turningRightPage = CreatePageInstance(enemyPagePrefab, turningPageRightRoot, turningRightEnemyPages, turningRightPageContents);

            SetupEnemyPage(leftPage, leftEnemyData, archiveManager);
            SetupEnemyPage(rightPage, rightEnemyData, archiveManager);
            SetupEnemyPage(turningLeftPage, leftEnemyData, archiveManager);
            SetupEnemyPage(turningRightPage, rightEnemyData, archiveManager);
        }
    }

    private EnemyInformationDataSO GetEnemyDataByIndex(List<EnemyInformationDataSO> unlockedEnemies, int index)
    {
        if (unlockedEnemies == null)
        {
            return null;
        }

        if (index < 0 || index >= unlockedEnemies.Count)
        {
            return null;
        }

        return unlockedEnemies[index];
    }

    private void SetupEnemyPage(EnemyPageUI page, EnemyInformationDataSO enemyData, IntelligenceArchiveManager archiveManager)
    {
        if (page == null)
        {
            return;
        }

        page.SetEnemyData(
            enemyData,
            archiveManager,
            unknownEnemySprite,
            lockedEnemyPictureSprite,
            unknownEnemyName,
            unknownEnemyInformationLine,
            unknownEnemyInformationLineCount
        );
    }

    private void BuildIntelligencePages(List<IntelligenceDataSO> unlockedIntelligences)
    {
        if (intelligencePagePrefab == null)
        {
            Debug.LogWarning("NoteBookUI 生成普通情报页失败：intelligencePagePrefab 没有赋值。");
            intelligenceSpreadCount = 1;
            return;
        }

        if (intelligencesPerPage <= 0)
        {
            intelligencesPerPage = 4;
        }

        int intelligenceCount = 0;

        if (unlockedIntelligences != null)
        {
            intelligenceCount = unlockedIntelligences.Count;
        }

        int intelligencesPerSpread = intelligencesPerPage * 2;

        intelligenceSpreadCount = Mathf.Max(1, Mathf.CeilToInt(intelligenceCount / (float)intelligencesPerSpread));

        for (int spreadIndex = 0; spreadIndex < intelligenceSpreadCount; spreadIndex++)
        {
            int leftStartIndex = spreadIndex * intelligencesPerSpread;
            int rightStartIndex = leftStartIndex + intelligencesPerPage;

            IntelligencePageUI leftPage = CreatePageInstance(intelligencePagePrefab, leftPageRoot, leftIntelligencePages, leftPageContents);
            IntelligencePageUI rightPage = CreatePageInstance(intelligencePagePrefab, rightPageRoot, rightIntelligencePages, rightPageContents);
            IntelligencePageUI turningLeftPage = CreatePageInstance(intelligencePagePrefab, turningPageLeftRoot, turningLeftIntelligencePages, turningLeftPageContents);
            IntelligencePageUI turningRightPage = CreatePageInstance(intelligencePagePrefab, turningPageRightRoot, turningRightIntelligencePages, turningRightPageContents);

            leftPage.SetPageData(unlockedIntelligences, leftStartIndex);
            rightPage.SetPageData(unlockedIntelligences, rightStartIndex);
            turningLeftPage.SetPageData(unlockedIntelligences, leftStartIndex);
            turningRightPage.SetPageData(unlockedIntelligences, rightStartIndex);
        }
    }

    private T CreatePageInstance<T>(T prefab, Transform parent, List<T> typedList, List<GameObject> contentList) where T : Component
    {
        if (prefab == null || parent == null)
        {
            return null;
        }

        T instance = Instantiate(prefab, parent);

        if (stretchInstantiatedPagesToParent)
        {
            StretchRectTransformToParent(instance.transform as RectTransform);
        }

        instance.gameObject.SetActive(false);

        typedList.Add(instance);
        contentList.Add(instance.gameObject);
        createdRuntimePageObjects.Add(instance.gameObject);

        return instance;
    }

    private void StretchRectTransformToParent(RectTransform rectTransform)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localScale = Vector3.one;
    }

    private void TryStartPageTurn(IEnumerator routine, bool keepBookMarkDecorationDuringTurn = false)
    {
        if (isTurning)
        {
            return;
        }

        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        pageTurnCoroutine = StartCoroutine(PageTurnWrapperRoutine(routine, keepBookMarkDecorationDuringTurn));
    }

    private IEnumerator PageTurnWrapperRoutine(IEnumerator routine, bool keepBookMarkDecorationDuringTurn)
    {
        isTurning = true;

        SetInputEnabled(false);

        // 普通翻页：隐藏所有导航按钮和装饰书签
        // 首页翻到内容页：隐藏按钮，但暂时保留装饰书签，等翻页盖住它时再隐藏
        HideNavigationButtons(keepBookMarkDecorationDuringTurn);

        yield return routine;

        HideTurningPagesImmediate();

        isTurning = false;
        pageTurnCoroutine = null;

        SetInputEnabled(true);
        RefreshNavigationButtons();
    }

    private IEnumerator FlipRightToSectionRoutine(NoteBookSection targetSection)
    {
        NoteBookSection fromSection = currentSection;
        int fromSpreadIndex = currentSpreadIndex;

        int targetSpreadIndex = 0;

        yield return PlayRightTurnRoutine(fromSection, fromSpreadIndex, targetSection, targetSpreadIndex);

        currentSection = targetSection;
        currentSpreadIndex = targetSpreadIndex;

        ShowCurrentMainPagesImmediate();
    }

    private IEnumerator FlipRightToNextSpreadRoutine()
    {
        NoteBookSection section = currentSection;

        int fromSpreadIndex = currentSpreadIndex;
        int targetSpreadIndex = currentSpreadIndex + 1;

        yield return PlayRightTurnRoutine(section, fromSpreadIndex, section, targetSpreadIndex);

        currentSpreadIndex = targetSpreadIndex;

        ShowCurrentMainPagesImmediate();
    }

    private IEnumerator FlipLeftToPreviousSpreadRoutine()
    {
        NoteBookSection section = currentSection;

        if (currentSpreadIndex <= 0)
        {
            yield return PlayLeftTurnRoutine(section, 0, NoteBookSection.Home, 0);

            currentSection = NoteBookSection.Home;
            currentSpreadIndex = 0;

            ShowHomePagesImmediate();
        }
        else
        {
            int fromSpreadIndex = currentSpreadIndex;
            int targetSpreadIndex = currentSpreadIndex - 1;

            yield return PlayLeftTurnRoutine(section, fromSpreadIndex, section, targetSpreadIndex);

            currentSpreadIndex = targetSpreadIndex;

            ShowCurrentMainPagesImmediate();
        }
    }

    private IEnumerator FlipLeftToHomeByBookMarkRoutine()
    {
        while (currentSection != NoteBookSection.Home)
        {
            NoteBookSection section = currentSection;

            if (currentSpreadIndex > 0)
            {
                int fromSpreadIndex = currentSpreadIndex;
                int targetSpreadIndex = currentSpreadIndex - 1;

                yield return PlayLeftTurnRoutine(section, fromSpreadIndex, section, targetSpreadIndex);

                currentSpreadIndex = targetSpreadIndex;

                ShowCurrentMainPagesImmediate();
            }
            else
            {
                yield return PlayLeftTurnRoutine(section, 0, NoteBookSection.Home, 0);

                currentSection = NoteBookSection.Home;
                currentSpreadIndex = 0;

                ShowHomePagesImmediate();
            }

            if (bookMarkTurnInterval > 0f && currentSection != NoteBookSection.Home)
            {
                yield return WaitForDurationRoutine(bookMarkTurnInterval);
            }
        }
    }

    private IEnumerator PlayRightTurnRoutine(NoteBookSection fromSection, int fromSpreadIndex, NoteBookSection targetSection, int targetSpreadIndex)
    {
        GameObject oldRightTurningContent = GetTurningRightContent(fromSection, fromSpreadIndex);
        GameObject targetRightMainContent = GetMainRightContent(targetSection, targetSpreadIndex);
        GameObject targetLeftTurningContent = GetTurningLeftContent(targetSection, targetSpreadIndex);
        GameObject targetLeftMainContent = GetMainLeftContent(targetSection, targetSpreadIndex);

        SetOnlyActive(rightPageContents, targetRightMainContent);

        PrepareTurningPage(turningPageRightRoot, turningRightPageContents, oldRightTurningContent, 1f);

        yield return AnimatePageScaleXRoutine(turningPageRightRoot, 1f, 0f);

        SafeSetActive(turningPageRightRoot.gameObject, false);

        PrepareTurningPage(turningPageLeftRoot, turningLeftPageContents, targetLeftTurningContent, 0f);

        yield return AnimatePageScaleXRoutine(turningPageLeftRoot, 0f, 1f);

        // 从首页翻到怪物页 / 情报页时：
        // 此时 TurningPageLeft 已经盖下来了，视觉上“内容页盖住了首页书签”
        // 所以在这里才把装饰书签换成真正可以点击的书签按钮
        if (fromSection == NoteBookSection.Home && targetSection != NoteBookSection.Home)
        {
            SwitchFromBookMarkDecorationToButton();
        }

        SafeSetActive(turningPageLeftRoot.gameObject, false);

        SetOnlyActive(leftPageContents, targetLeftMainContent);
    }

    private IEnumerator PlayLeftTurnRoutine(NoteBookSection fromSection, int fromSpreadIndex, NoteBookSection targetSection, int targetSpreadIndex)
    {
        GameObject oldLeftTurningContent = GetTurningLeftContent(fromSection, fromSpreadIndex);
        GameObject targetLeftMainContent = GetMainLeftContent(targetSection, targetSpreadIndex);
        GameObject targetRightTurningContent = GetTurningRightContent(targetSection, targetSpreadIndex);
        GameObject targetRightMainContent = GetMainRightContent(targetSection, targetSpreadIndex);

        // 从怪物页 / 情报页翻回首页时：
        // 刚开始翻左页，视觉上首页的完整书签就应该露出来
        // 所以这里立刻禁用真正的书签按钮，并启用装饰书签
        if (fromSection != NoteBookSection.Home && targetSection == NoteBookSection.Home)
        {
            SwitchFromBookMarkButtonToDecoration();
        }

        SetOnlyActive(leftPageContents, targetLeftMainContent);

        PrepareTurningPage(turningPageLeftRoot, turningLeftPageContents, oldLeftTurningContent, 1f);

        yield return AnimatePageScaleXRoutine(turningPageLeftRoot, 1f, 0f);

        SafeSetActive(turningPageLeftRoot.gameObject, false);

        PrepareTurningPage(turningPageRightRoot, turningRightPageContents, targetRightTurningContent, 0f);

        yield return AnimatePageScaleXRoutine(turningPageRightRoot, 0f, 1f);

        SafeSetActive(turningPageRightRoot.gameObject, false);

        SetOnlyActive(rightPageContents, targetRightMainContent);
    }

    private void PrepareTurningPage(RectTransform turningRoot, List<GameObject> turningContents, GameObject activeContent, float scaleX)
    {
        if (turningRoot == null)
        {
            return;
        }

        SafeSetActive(turningRoot.gameObject, true);
        SetOnlyActive(turningContents, activeContent);
        SetRectTransformScaleX(turningRoot, scaleX);
    }

    private IEnumerator AnimatePageScaleXRoutine(RectTransform targetRectTransform, float fromScaleX, float toScaleX)
    {
        if (targetRectTransform == null)
        {
            yield break;
        }

        float timer = 0f;

        SetRectTransformScaleX(targetRectTransform, fromScaleX);

        while (timer < halfTurnDuration)
        {
            float progress = halfTurnDuration <= 0f ? 1f : timer / halfTurnDuration;
            float evaluatedProgress = pageTurnCurve.Evaluate(progress);
            float scaleX = Mathf.LerpUnclamped(fromScaleX, toScaleX, evaluatedProgress);

            SetRectTransformScaleX(targetRectTransform, scaleX);

            timer += GetDeltaTime();

            yield return null;
        }

        SetRectTransformScaleX(targetRectTransform, toScaleX);
    }

    private IEnumerator WaitForDurationRoutine(float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += GetDeltaTime();
            yield return null;
        }
    }

    private float GetDeltaTime()
    {
        if (useUnscaledTime)
        {
            return Time.unscaledDeltaTime;
        }

        return Time.deltaTime;
    }

    private void SetRectTransformScaleX(RectTransform rectTransform, float scaleX)
    {
        if (rectTransform == null)
        {
            return;
        }

        Vector3 scale = rectTransform.localScale;
        scale.x = scaleX;
        rectTransform.localScale = scale;
    }

    private void ShowHomePagesImmediate()
    {
        currentSection = NoteBookSection.Home;
        currentSpreadIndex = 0;

        SetOnlyActive(leftPageContents, leftHomePageContent);
        SetOnlyActive(rightPageContents, rightHomePageContent);
    }

    private void ShowCurrentMainPagesImmediate()
    {
        GameObject leftContent = GetMainLeftContent(currentSection, currentSpreadIndex);
        GameObject rightContent = GetMainRightContent(currentSection, currentSpreadIndex);

        SetOnlyActive(leftPageContents, leftContent);
        SetOnlyActive(rightPageContents, rightContent);
    }

    private GameObject GetMainLeftContent(NoteBookSection section, int spreadIndex)
    {
        if (section == NoteBookSection.Home)
        {
            return leftHomePageContent;
        }

        if (section == NoteBookSection.Enemy)
        {
            return GetContentByIndex(leftEnemyPages, spreadIndex);
        }

        if (section == NoteBookSection.Intelligence)
        {
            return GetContentByIndex(leftIntelligencePages, spreadIndex);
        }

        return null;
    }

    private GameObject GetMainRightContent(NoteBookSection section, int spreadIndex)
    {
        if (section == NoteBookSection.Home)
        {
            return rightHomePageContent;
        }

        if (section == NoteBookSection.Enemy)
        {
            return GetContentByIndex(rightEnemyPages, spreadIndex);
        }

        if (section == NoteBookSection.Intelligence)
        {
            return GetContentByIndex(rightIntelligencePages, spreadIndex);
        }

        return null;
    }

    private GameObject GetTurningLeftContent(NoteBookSection section, int spreadIndex)
    {
        if (section == NoteBookSection.Home)
        {
            return turningLeftHomePageContent;
        }

        if (section == NoteBookSection.Enemy)
        {
            return GetContentByIndex(turningLeftEnemyPages, spreadIndex);
        }

        if (section == NoteBookSection.Intelligence)
        {
            return GetContentByIndex(turningLeftIntelligencePages, spreadIndex);
        }

        return null;
    }

    private GameObject GetTurningRightContent(NoteBookSection section, int spreadIndex)
    {
        if (section == NoteBookSection.Home)
        {
            return turningRightHomePageContent;
        }

        if (section == NoteBookSection.Enemy)
        {
            return GetContentByIndex(turningRightEnemyPages, spreadIndex);
        }

        if (section == NoteBookSection.Intelligence)
        {
            return GetContentByIndex(turningRightIntelligencePages, spreadIndex);
        }

        return null;
    }

    private GameObject GetContentByIndex<T>(List<T> pageList, int index) where T : Component
    {
        if (pageList == null)
        {
            return null;
        }

        if (index < 0 || index >= pageList.Count)
        {
            return null;
        }

        if (pageList[index] == null)
        {
            return null;
        }

        return pageList[index].gameObject;
    }

    private int GetCurrentSectionSpreadCount()
    {
        if (currentSection == NoteBookSection.Enemy)
        {
            return enemySpreadCount;
        }

        if (currentSection == NoteBookSection.Intelligence)
        {
            return intelligenceSpreadCount;
        }

        return 0;
    }

    private void SetOnlyActive(List<GameObject> contentList, GameObject activeContent)
    {
        if (contentList == null)
        {
            return;
        }

        for (int i = 0; i < contentList.Count; i++)
        {
            GameObject content = contentList[i];

            if (content == null)
            {
                continue;
            }

            content.SetActive(content == activeContent);
        }
    }

    private void HideTurningPagesImmediate()
    {
        if (turningPageLeftRoot != null)
        {
            SetRectTransformScaleX(turningPageLeftRoot, 1f);
            turningPageLeftRoot.gameObject.SetActive(false);
        }

        if (turningPageRightRoot != null)
        {
            SetRectTransformScaleX(turningPageRightRoot, 1f);
            turningPageRightRoot.gameObject.SetActive(false);
        }
    }

    private void SetInputEnabled(bool enable)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.blocksRaycasts = enable;
    }

    private void RefreshNavigationButtons()
    {
        bool isHome = currentSection == NoteBookSection.Home;

        SafeSetActive(bookMarkButtonDecoration, isHome);

        if (bookMarkButton != null)
        {
            SafeSetActive(bookMarkButton.gameObject, !isHome);
        }

        if (lastPageButton != null)
        {
            SafeSetActive(lastPageButton.gameObject, !isHome);
        }

        if (nextPageButton != null)
        {
            bool canGoNext = !isHome && currentSpreadIndex < GetCurrentSectionSpreadCount() - 1;
            SafeSetActive(nextPageButton.gameObject, canGoNext);
        }
    }

    private void HideNavigationButtons(bool keepBookMarkDecoration = false)
    {
        if (!keepBookMarkDecoration)
        {
            SafeSetActive(bookMarkButtonDecoration, false);
        }

        if (bookMarkButton != null)
        {
            SafeSetActive(bookMarkButton.gameObject, false);
        }

        if (lastPageButton != null)
        {
            SafeSetActive(lastPageButton.gameObject, false);
        }

        if (nextPageButton != null)
        {
            SafeSetActive(nextPageButton.gameObject, false);
        }
    }

    private void SwitchFromBookMarkDecorationToButton()
    {
        SafeSetActive(bookMarkButtonDecoration, false);

        if (bookMarkButton != null)
        {
            SafeSetActive(bookMarkButton.gameObject, true);
        }
    }

    private void SwitchFromBookMarkButtonToDecoration()
    {
        if (bookMarkButton != null)
        {
            SafeSetActive(bookMarkButton.gameObject, false);
        }

        SafeSetActive(bookMarkButtonDecoration, true);
    }

    private void SafeSetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    private void StopCurrentTurnCoroutine()
    {
        if (pageTurnCoroutine != null)
        {
            StopCoroutine(pageTurnCoroutine);
            pageTurnCoroutine = null;
        }
    }

    private IEnumerator AutoOpenToUnlockedEntryRoutine(ArchiveUnlockRecord unlockRecord)
    {
        isTurning = true;

        SetInputEnabled(false);

        NoteBookSection targetSection = GetTargetSectionByUnlockRecord(unlockRecord);
        int targetSpreadIndex = GetTargetSpreadIndexByUnlockRecord(unlockRecord, targetSection);

        // 自动从首页进入对应分类时，也要保留首页装饰书签，
        // 等 TurningPageLeft 盖下去后再换成真正的书签按钮
        HideNavigationButtons(true);

        yield return PlayRightTurnRoutine(NoteBookSection.Home, 0, targetSection, 0);

        currentSection = targetSection;
        currentSpreadIndex = 0;

        ShowCurrentMainPagesImmediate();

        while (currentSpreadIndex < targetSpreadIndex)
        {
            if (autoFocusTurnInterval > 0f)
            {
                yield return WaitForDurationRoutine(autoFocusTurnInterval);
            }

            HideNavigationButtons(false);

            int nextSpreadIndex = currentSpreadIndex + 1;

            yield return PlayRightTurnRoutine(currentSection, currentSpreadIndex, currentSection, nextSpreadIndex);

            currentSpreadIndex = nextSpreadIndex;

            ShowCurrentMainPagesImmediate();
        }

        HideTurningPagesImmediate();

        isTurning = false;
        pageTurnCoroutine = null;

        RefreshNavigationButtons();

        SetInputEnabled(true);
    }

    private NoteBookSection GetTargetSectionByUnlockRecord(ArchiveUnlockRecord unlockRecord)
    {
        if (unlockRecord == null)
        {
            return NoteBookSection.Home;
        }

        if (unlockRecord.unlockType == ArchiveUnlockType.Intelligence)
        {
            return NoteBookSection.Intelligence;
        }

        return NoteBookSection.Enemy;
    }

    private int GetTargetSpreadIndexByUnlockRecord(ArchiveUnlockRecord unlockRecord, NoteBookSection targetSection)
    {
        if (unlockRecord == null)
        {
            return 0;
        }

        if (targetSection == NoteBookSection.Intelligence)
        {
            int intelligenceIndex = GetIntelligenceIndex(unlockRecord.intelligenceData);

            if (intelligenceIndex < 0)
            {
                return 0;
            }

            int intelligencesPerSpread = Mathf.Max(1, intelligencesPerPage * 2);
            return intelligenceIndex / intelligencesPerSpread;
        }

        if (targetSection == NoteBookSection.Enemy)
        {
            int enemyIndex = GetEnemyIndex(unlockRecord.enemyInformationData);

            if (enemyIndex < 0)
            {
                return 0;
            }

            return enemyIndex / 2;
        }

        return 0;
    }

    private int GetIntelligenceIndex(IntelligenceDataSO intelligenceData)
    {
        if (intelligenceData == null)
        {
            return -1;
        }

        for (int i = 0; i < currentUnlockedIntelligences.Count; i++)
        {
            IntelligenceDataSO currentData = currentUnlockedIntelligences[i];

            if (currentData == intelligenceData)
            {
                return i;
            }

            if (currentData != null && currentData.SaveID == intelligenceData.SaveID)
            {
                return i;
            }
        }

        return -1;
    }

    private int GetEnemyIndex(EnemyInformationDataSO enemyInformationData)
    {
        if (enemyInformationData == null)
        {
            return -1;
        }

        for (int i = 0; i < currentUnlockedEnemies.Count; i++)
        {
            EnemyInformationDataSO currentData = currentUnlockedEnemies[i];

            if (currentData == enemyInformationData)
            {
                return i;
            }

            if (currentData != null && currentData.SaveID == enemyInformationData.SaveID)
            {
                return i;
            }
        }

        return -1;
    }
}