using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>4단계의 화면 출력 구조를 복습하는 5문제 OX 퀴즈입니다.</summary>
public class RuntimeScreenOutputQuiz : MonoBehaviour
{
    private readonly string[] questions =
    {
        "16:9 화면은 4:3 화면보다 가로 비율이 더 넓다.",
        "Universal Render Pipeline/Unlit Shader는 Scene Light의 영향을 받아 밝기가 변한다.",
        "Quad는 네 개의 정점으로 만든 평면 사각형 Mesh이다.",
        "Main Camera의 Output Texture에 Render Texture를 넣으면 Main Camera가 Display 1에 직접 출력한다.",
        "화면이 나오지 않으면 Camera 활성화, Culling Mask, Material Texture 연결을 확인해야 한다."
    };

    private readonly bool[] answers = { true, false, true, false, true };
    private Font uiFont;
    private bool wasMissionFour;
    private bool quizRunning;
    private bool quizFinished;
    private bool waitingForNext;
    private int currentQuestion;
    private int correctCount;
    private string feedback = "";
    private float nextQuestionTime;

    public bool IsSucceeded { get; private set; }

    private void Start()
    {
        uiFont = Resources.Load<Font>("Fonts/NanumGothic-Regular");
    }

    private void Update()
    {
        bool missionFour = CameraLearningTutorial.CurrentMission == 4;
        if (missionFour && !wasMissionFour)
            ResetQuiz();
        wasMissionFour = missionFour;
        if (!missionFour || Keyboard.current == null)
            return;

        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            StartQuiz();
            return;
        }

        if (!quizRunning)
            return;
        if (waitingForNext)
        {
            if (Time.unscaledTime >= nextQuestionTime)
                MoveToNextQuestion();
            return;
        }

        if (Keyboard.current.oKey.wasPressedThisFrame)
            SubmitAnswer(true);
        else if (Keyboard.current.xKey.wasPressedThisFrame)
            SubmitAnswer(false);
    }

    private void ResetQuiz()
    {
        quizRunning = false;
        quizFinished = false;
        waitingForNext = false;
        currentQuestion = 0;
        correctCount = 0;
        feedback = "";
        IsSucceeded = false;
    }

    private void StartQuiz()
    {
        quizRunning = true;
        quizFinished = false;
        waitingForNext = false;
        currentQuestion = 0;
        correctCount = 0;
        feedback = "";
        IsSucceeded = false;
    }

    private void SubmitAnswer(bool playerAnswer)
    {
        bool correct = playerAnswer == answers[currentQuestion];
        if (correct)
        {
            correctCount++;
            feedback = "O  정답입니다.";
        }
        else
        {
            feedback = $"X  오답입니다. 정답: {(answers[currentQuestion] ? "O" : "X")}";
        }
        waitingForNext = true;
        nextQuestionTime = Time.unscaledTime + 1.1f;
    }

    private void MoveToNextQuestion()
    {
        waitingForNext = false;
        feedback = "";
        currentQuestion++;
        if (currentQuestion < questions.Length)
            return;

        quizRunning = false;
        quizFinished = true;
        IsSucceeded = correctCount >= 3;
    }

    private void OnGUI()
    {
        if (CameraLearningTutorial.CurrentMission != 4)
            return;
        if (uiFont != null)
            GUI.skin.font = uiFont;

        float width = Mathf.Min(680f, Screen.width - 40f);
        const float height = 190f;
        float x = (Screen.width - width) * 0.5f;
        float y = Screen.height * 0.5f - 20f;
        GUI.Box(new Rect(x, y, width, height), "SCREEN OUTPUT · OX QUIZ");

        if (!quizRunning && !quizFinished)
        {
            GUI.Label(new Rect(x + 24f, y + 45f, width - 48f, 32f), "T를 눌러 5문제 OX 퀴즈를 시작하세요.");
            GUI.Label(new Rect(x + 24f, y + 84f, width - 48f, 50f), "O/X 버튼 또는 O/X 키로 답합니다.\n5문제 중 3문제 이상 맞히면 목표 달성");
            return;
        }

        if (quizFinished)
        {
            string result = IsSucceeded
                ? $"O  퀴즈 통과 · {correctCount}/5 정답"
                : $"X  퀴즈 실패 · {correctCount}/5 정답";
            GUI.Label(new Rect(x + 24f, y + 48f, width - 48f, 36f), result);
            GUI.Label(new Rect(x + 24f, y + 94f, width - 48f, 32f),
                IsSucceeded ? "목표를 달성했습니다." : "T를 눌러 다시 도전하세요.");
            return;
        }

        GUI.Label(new Rect(x + 24f, y + 38f, width - 48f, 26f),
            $"문제 {currentQuestion + 1}/5   |   현재 정답 {correctCount}개");
        GUI.Label(new Rect(x + 24f, y + 70f, width - 48f, 52f), questions[currentQuestion]);
        if (waitingForNext)
        {
            GUI.Label(new Rect(x + 24f, y + 128f, width - 48f, 32f), feedback);
        }
        else
        {
            float buttonWidth = 130f;
            float gap = 24f;
            float buttonX = x + (width - buttonWidth * 2f - gap) * 0.5f;
            if (GUI.Button(new Rect(buttonX, y + 126f, buttonWidth, 44f), "O  맞다"))
                SubmitAnswer(true);
            if (GUI.Button(new Rect(buttonX + buttonWidth + gap, y + 126f, buttonWidth, 44f), "X  틀리다"))
                SubmitAnswer(false);
        }
    }
}
