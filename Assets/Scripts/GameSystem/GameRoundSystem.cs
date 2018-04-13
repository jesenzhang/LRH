
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VFrame.GameTools;
using VFrame.ABSystem;
using VFrame.UI;
using System;
using EnumProperty;


public class GameRoundSystem : MonoBehaviour
{
    enum RoundState
    {
        None = 0,
        Begin = 1,
        PlayerRurn = 2,
        NpcTurn = 3,
        End = 4
    }
    public static GameRoundSystem Instance;
    private void Awake()
    {
        Instance = this;
    }
    //关卡信息
    public LevelData levelData;
    //关卡规则类型对象
    public RoundProperty roundData;

    //对局中的变量
    //系统数据读取完成标识
    private bool DataReady = false;
    //当前回合
    public int CurrentRound = 0;
    //当前回合进行到的步数
    public int CurrentStep = 0;
    //当前关卡
    public int CurrentLevel = 0;
    //当前状态最大步数
    public int MaxStep = 0;
    //当前关卡最大回合数
    public int MaxRound = 0;
    //关卡的总数
    public int MaxLevel = 0;

    //选择合作的次数
    public int DoNum = 0;
    //选择欺骗的次数
    public int UnDoNum = 0;
    //使用的第一、二、三张卡牌的id 
    //1是增加金钱 2 是调查 3 是影响NPC概率的牌
    public int card1;
    public int card2;
    public int card3;
    //使用增加一方金钱卡时选择的自己 还是 NPC
    public int ChooseSide = 0;
    //NPC友好度
    public float friendly = 0;
    //NPC的合作概率
    public float Rate=0;
    //玩家金钱
	public int Money=0;
    //强制设置的NPC合作概率
	public float ForceNPCRate = -1;
    //当前关卡剩余的回合
    public int RemainRound
    {
        get
        {
            return levelData.rounds.Length - CurrentRound;
        }
    }

    //解析规则配置的临时数组
    private List<RoundGoalProfitItem> allPerseItem = new List<RoundGoalProfitItem>();

    //解析支付规则字符串
    public void PerseRule(ref List<RoundGoalProfitItem> allitems, string input = "-50=B<0=D<A=9<C=100")
    {
        allitems.Clear();
        int N = input.Length;
        int start = 0;
        int length = 0;
        for (int i = 0; i < N;)
        {
            char current = input[i];
            if (current == '-' || current >= '0' && current <= '9')
            {
                i++; length++;
                if (i >= N)
                {
                    i = N - 1;
                    string num = input.Substring(start, N - start);
                    int Num = int.Parse(num);
                    RoundGoalProfitItem item = new RoundGoalProfitItem
                    {
                        ProfitType = EnumProperty.RoundProfitType.Number,
                        Value = Num
                    };
                    allitems.Add(item);
                    length = 0; start = i; i = N;
                }
                else
                {
                    char next = input[i];
                    if (next < '0' || next > '9')
                    {
                        string num = input.Substring(start, length);
                        int Num = int.Parse(num);
                        RoundGoalProfitItem item = new RoundGoalProfitItem
                        {
                            ProfitType = EnumProperty.RoundProfitType.Number,
                            Value = Num
                        };
                        allitems.Add(item); length = 0; start = i;
                    }
                }
            }
            else
            {
                if (current == '>' || current == '<' || current == '=')
                {
                    if (input[i + 1] == '=' && current != '=')
                    {
                        i = i + 2; length = 2;
                    }
                    else
                    {
                        i++; length = 1;
                    }
                    string sign = input.Substring(start, length);
                    RoundGoalProfitItem item = new RoundGoalProfitItem
                    {
                        ProfitType = EnumProperty.RoundProfitType.Sign
                    };
                    switch (sign)
                    {
                        case ">=": item.Sign = EnumProperty.RoundProfitSign.NotLess; break;
                        case ">": item.Sign = EnumProperty.RoundProfitSign.More; break;
                        case "<": item.Sign = EnumProperty.RoundProfitSign.Less; break;
                        case "<=": item.Sign = EnumProperty.RoundProfitSign.NotMore; break;
                        case "=": item.Sign = EnumProperty.RoundProfitSign.Equal; break;
                        default: item.Sign = EnumProperty.RoundProfitSign.None; break;
                    }
                    allitems.Add(item);
                    start = i; length = 0;
                }
                else
                {
                    if (current >= 'A' && current <= 'H')
                    {
                        i++; length = 1;
                        string pay = input.Substring(start, length);
                        RoundGoalProfitItem item = new RoundGoalProfitItem
                        {
                            ProfitType = EnumProperty.RoundProfitType.RolePay,
                            RolePay = (EnumProperty.RoundProfitRolePay)(current - 'A' + 1)
                        };
                        allitems.Add(item);
                        start = i; length = 0;
                    }
                }
            }
        }
    }
    //填充规则Profit
    public void FillProfit(ref Vector2[] Profit, ref List<RoundGoalProfitItem> allitems)
    {
        for (int i = 0; i < allitems.Count; i++)
        {
            RoundGoalProfitItem item = allitems[i];
            int Left = 0; int Right = 0; int numless = 0; int nummore = 0; bool LeftEqual = false; bool RightEqual = false;
            if (item.ProfitType == EnumProperty.RoundProfitType.RolePay)
            {
                for (int k = i - 1; k >= 0; k--)
                {
                    RoundGoalProfitItem item2 = allitems[k];
                    if (k == i - 1 && item2.ProfitType == EnumProperty.RoundProfitType.Sign && item2.Sign == EnumProperty.RoundProfitSign.Equal)
                    {
                        RoundGoalProfitItem item3 = allitems[i - 2];
                        if (item3.ProfitType == EnumProperty.RoundProfitType.Number)
                        {
                            LeftEqual = true; Left = item3.Value;
                            break;
                        }
                    }
                    if (item2.ProfitType == EnumProperty.RoundProfitType.Sign && item2.Sign == EnumProperty.RoundProfitSign.Less)
                    {
                        nummore++;
                    }
                    if (item2.ProfitType == EnumProperty.RoundProfitType.Number)
                    {
                        Left = item2.Value + nummore;
                        break;
                    }
                }

                for (int j = i + 1; j < allitems.Count; j++)
                {
                    RoundGoalProfitItem item2 = allitems[j];
                    if (j == i + 1 && item2.ProfitType == EnumProperty.RoundProfitType.Sign && item2.Sign == EnumProperty.RoundProfitSign.Equal)
                    {
                        RoundGoalProfitItem item3 = allitems[i + 2];
                        if (item3.ProfitType == EnumProperty.RoundProfitType.Number)
                        {
                            RightEqual = true;
                            Right = item3.Value;
                            break;
                        }
                        RightEqual = true;
                    }
                    if (item2.ProfitType == EnumProperty.RoundProfitType.Sign && item2.Sign == EnumProperty.RoundProfitSign.Less)
                    {
                        numless++;
                    }
                    if (item2.ProfitType == EnumProperty.RoundProfitType.Number)
                    {
                        Right = item2.Value - numless;
                        break;
                    }
                }
                if (Left > Right)
                {
                    Debug.LogError("Left > Right Check the Data");
                    return;
                }
                int index = (int)item.RolePay; int f = (index - 1) % 4; int xy = (index - 1) / 4; int rnum = 0;
                if (LeftEqual)
                    rnum = Left;
                else if (RightEqual)
                    rnum = Right;
                else
                {
                    rnum = MathUtil.GetRandom(Left, Right);
                }
                if (xy == 0) Profit[f].x = rnum;
                if (xy == 1) Profit[f].y = rnum;
                item.ProfitType = EnumProperty.RoundProfitType.Number;
                item.RolePay = EnumProperty.RoundProfitRolePay.None;
                item.Value = rnum;
            }
        }
    }

    //赋值给UI界面的数据对象
    object[] UIRoundData;
    //剩余的步数
    public int NextActions = 0;

    //加载数据
    public void LoadAsset(int level)
    {
        DataReady = false;

        //读取关卡规则信息
        if (level < GameData.Instance.SystemData.AllRounds.Length)
        {
            roundData = (RoundProperty)(GameData.Instance.SystemData.AllRounds[level]).Clone();
        }
        //读取关卡的回合数 步骤等信息
        if (level < GameData.Instance.SystemData.AllLevels.Length)
        {
           
            levelData = (LevelData)(GameData.Instance.SystemData.AllLevels[level]).Clone();
            MaxRound = levelData.rounds.Length;
            InitData();
            DataReady = true;
            //设置UI显示数据
			UIRoundData = new object[4] { roundData, RemainRound,StepType.None,level};
            //显示UI
            UIManager.Instance.ShowUI<UIGameRound>(null, UIRoundData);
            StartCoroutine(StartBattle());
        }
    }

    //初始化数据
    public void InitData()
    {
		if(roundData.NeedPerse)
		{
        for (int i = 0; i < roundData.Rules.Length; i++)
        {
            PerseRule(ref allPerseItem, roundData.Rules[i]);
            FillProfit(ref roundData.Profit, ref allPerseItem);
			}
		}
        MaxLevel = GameData.Instance.SystemData.AllLevels.Length;
    }

    //开始
    public void BeginRound()
    {
        StartCoroutine(StartBattle());
    }

    //开始回合 初始化数据
    public IEnumerator StartBattle()
    {
        yield return new WaitUntil(() => { return DataReady; });
        yield return new WaitUntil(() => {
            return true;
        });
        CurrentRound = 0;
        CurrentStep = 0;
        InitStep();
    }
    //计算友好度
    public float Friendly {
        get {
			float f=0;
            if (DoNum + UnDoNum > 0)
            {
                float r = UnDoNum / (DoNum + UnDoNum);
                if (r >= 0.6f)
                {
                    f = 0.6f * (2 - r);
                }
                else
                {
                    f = 0.6f * (1 - r);
                }
            }
			f = f+friendly;
            return f;
           
        }
    }
    //计算荣誉值
    public int Hornor
    {
        get
        {
            return 50 - UnDoNum * 10 + DoNum * 5;
        }
    }
    //计算NPC合作概率
    public float NPCRate {
        get {
			if(ForceNPCRate>0)
				return ForceNPCRate;
			float a2 = roundData.Profit[0].y;
			float b2 = roundData.Profit[1].y;
			float c2 = roundData.Profit[2].y;
			float d2 = roundData.Profit[3].y;
			float p =0;
			if(a2>b2 && c2>d2)
			{
				p=1;
			}else if(a2<b2 && c2<d2)
			{
				p=0;
			}else
			{

            float a1 = roundData.Profit[0].x;
            float b1 = roundData.Profit[1].x;
            float c1 = roundData.Profit[2].x;
            float d1 = roundData.Profit[3].x;
            p = (d1 - b1) / (a1 - b1 - c1 + d1)+Rate;
			}
			if(p>1)
				p=1;
            return p;
        }
    }
    //做后一回合的NPC合作概率
    public float FinalNPCRate
    {
        get
        {
            float p = 0;
            if (DoNum + UnDoNum > 0)
            {
                float r = UnDoNum / (DoNum + UnDoNum);
                if (r >= 0.6f)
                {
                    p = NPCRate*Friendly;
                }
                else
                {
                    p = NPCRate * (1+Friendly);
                }
            }
			if(p>1)
				p=1;
            return p;
        }
    }
    //返回决策之后玩家的金钱
    public int EarnMoney(int type)
    {
		Money = Money+(int)roundData.Profit[type - 1].x;
		return Money;
    }
    //使用卡片 
    public void UseCard(int card)
    {
        if (card == 1)
        {
            int money = GameData.Instance.SystemData.GameAllCards[card1].Values[0];
			Money+=money;
        }
        if (card == 2)
        {
            int money = GameData.Instance.SystemData.GameAllCards[card2].Values[0];
            int friend = GameData.Instance.SystemData.GameAllCards[card2].Values[1];
			friendly+=(float)friend/100f;
        }
        if (card == 3)
        {
			
        }

        DoStep();
    }
    //点击使用增加玩家金钱卡牌按钮
    public void Btn_PlayerClicked()
    {
        UseCard(1);
    } 
    //点击使用增加NPC金钱卡牌按钮
    public void Btn_NPCClicked()
    {
        UseCard(2);
    }

    //根据玩家选择得到决策的结果
    public int GetResult(bool playerchoose)
    {
        int resur = 1;
        bool NpcChoose = false;
        int p = MathUtil.GetRandom(1, 100);
        if (p < NPCRate * 100)
        {
            NpcChoose = true;
        }
        else
        {
            NpcChoose = false;
        }
        if (playerchoose && NpcChoose)
            resur = 1;
        if (playerchoose && !NpcChoose)
            resur = 2;
        if (!playerchoose && NpcChoose)
            resur = 3;
        if (!playerchoose && !NpcChoose)
            resur = 4;
        return resur;
    }

    //点击合作安妮
    public void Btn_DoClicked()
    {
        DoNum++;
       
        StartCoroutine(ShowResult(GetResult(true)));
       
    }
    //点击欺骗按钮
    public void Btn_UnDoClicked()
    {
        UnDoNum++;
        StartCoroutine(ShowResult(GetResult(false)));
    }
 
    //点击使用调查卡牌
    public void Btn_UseCard2Clicked()
    {
		if(GameData.Instance.SystemData.GameAllCards[card3].Values.Length>0){
			ForceNPCRate = ((float)GameData.Instance.SystemData.GameAllCards[card3].Values[0])/100f;
		}
		UIManager.Instance.ShowUI<UINotice>(()=> {UseCard(3); }, "这个NPC有"+NPCRate*100+"%概率选择合作！");
    }
    //点击下一步按钮
    public void Btn_NextClicked()
    {
        DoStep();
    }
    //显示结果
    public IEnumerator ShowResult(int type)
    {
        UIRoundData[2] = StepType.Result;
        UIGameRound roundUI = (UIGameRound)UIManager.Instance.GetPageInstatnce<UIGameRound>();
        roundUI.UpdateDataShow();
        string show = type == 1 ? "结果公布：己方合作，对方合作" : type == 2 ? "结果公布：己方合作，对方不合作":type ==3 ? "结果公布：己方不合作，对方合作" : "结果公布：己方不合作，对方不合作";
        roundUI.SetResult(show, EarnMoney(type));
        yield return new WaitForSeconds(3);
        DoStep();
    }

    //初始化当前步骤
    public void InitStep()
    {
        RoundStep step = levelData.rounds[CurrentRound].steps[CurrentStep];
        UIRoundData[2] = step.stepType;
        UIRoundData[1] = (object)RemainRound;
        MaxStep = levelData.rounds[CurrentRound].steps.Length;
        UIGameRound roundUI = (UIGameRound)UIManager.Instance.GetPageInstatnce<UIGameRound>();
        //特殊步骤的特殊配置数据
        if (step.stepType == StepType.Think)
        {
            roundUI.SetFriend("判定你的名誉值为"+Hornor, (int)FinalNPCRate * 100);
        }
        roundUI.UpdateDataShow();
        if (step.stepType == StepType.UseCard)
        {
            NextActions = 2;
            card1 = step.CardList[0];
            card2 = step.CardList[1];
            card3 = step.CardList[2];
        }
        else
            NextActions = 1;

    }
    //执行一个步骤
    public void DoStep()
    {
        NextActions--;
        CheckStep();
    }

    /// <summary>
    /// 检查是否结束
    /// </summary>
    public void CheckStep()
    {
        //当前步骤所有操作执行完
        if (NextActions <= 0)
        {
            //步骤加一 初始化
            CurrentStep++;
            if (CurrentStep < MaxStep)
            {
                InitStep();
            }
        }
        //当前回合所有步骤执行完
        if (CurrentStep >= MaxStep)
        {
            //重置步骤 增加回合 初始化
            CurrentStep = 0;
            CurrentRound++;
            if(CurrentRound < MaxRound)
                InitStep();
        }
        //所有回合结束 结束关卡
        if (CurrentRound >= MaxRound)
        {
            CurrentRound = 0;
            EndLevel();
        }

    }
    //重置变量
	public  void ResetData()
	{
		CurrentRound = 0;
		CurrentStep = 0;
		CurrentLevel = 0;
		MaxStep = 0;
		MaxRound = 0;
		MaxLevel = 0;
		DoNum = 0;
		UnDoNum = 0;
		card1=0;
		card2=0;
		card3=0;
		ChooseSide = 0;
		friendly = 0;
		Rate=0;
		Money=0;
		ForceNPCRate=-1;
	}
    //结束关卡
    public void EndLevel()
    {
        CurrentLevel++;
       
        //判断关卡结果
			if(Money>= levelData.goal)
			{
				if (CurrentLevel < MaxLevel)
				{
					UIManager.Instance.ShowUI<UINotice>(()=>{
					friendly = 0;
					Rate=0;
					Money=0;
					ForceNPCRate=-1;
					LoadAsset(CurrentLevel);},"过关了！");
				}
				else
				{
					UIManager.Instance.ShowUI<UINotice>(()=>{
					ResetData();
					LoadAsset(CurrentLevel);},"通关了！");
				}
			}else
			{
				UIManager.Instance.ShowUI<UINotice>(()=>{
					ResetData();
					LoadAsset(CurrentLevel);},"失败了！");
			}
    }
}
