using System;
using System.Collections.Generic;
using Klei.AI;
using KSerialization;
using STRINGS;
using TUNING;
using UnityEngine;
using static DebuffRoulette.ModPatch;

public class BuildingBrain : Workable
{
	
	public bool WorkComplete
	{
		get
		{
            // 检查 geneShufflerSMI 是否处于 sm.working.complete 状态中。处于这个状态，WorkComplete 属性将返回 true，否则返回 false
            return this.geneShufflerSMI.IsInsideState(this.geneShufflerSMI.sm.working.complete);
		}
	}

	public bool IsWorking
	{
		get
		{
			return this.geneShufflerSMI.IsInsideState(this.geneShufflerSMI.sm.working);
		}
	}



	protected override void OnPrefabInit()
	{                                                                                                                             
		base.OnPrefabInit();
		this.assignable.OnAssign += this.Assign;
		this.lightEfficiencyBonus = false;
	}
    private void Assign(IAssignableIdentity new_assignee)
    {
        this.CancelChore();
        if (new_assignee != null)
        {
            this.ActivateChore();
        }
    }
    private void CancelChore()
    {
        if (this.chore == null)
        {
            return;
        }
        this.chore.Cancel("User cancelled");
        this.chore = null;
    }
    private void ActivateChore()
    {
        global::Debug.Assert(this.chore == null);
        base.GetComponent<Workable>().SetWorkTime(float.PositiveInfinity);
        this.chore = new WorkChore<Workable>(AddNewChorePatch.Accepttheinheritance, this, null, true, delegate (Chore o)
        {
            this.CompleteChore();
        }, null, null, true, null, false, true, Assets.GetAnim("anim_interacts_neuralvacillator_kanim"), false, true, false, PriorityScreen.PriorityClass.high, 5, false, true);
    }
    private void CompleteChore()
    {
        this.chore.Cleanup();
        this.chore = null;
    }




    protected override void OnSpawn()
	{
		base.OnSpawn();

		this.showProgressBar = false;
		this.geneShufflerSMI = new BuildingBrainSM.Instance(this);

        this.SetConsumed(true); // 初始化使其需要补充
        this.RequestRecharge(false);//  初始化不请求运送

        this.RefreshRechargeChore();
		this.RefreshConsumedState();
		base.Subscribe<BuildingBrain>(-1697596308, OnStorageChangeDelegate);
		this.geneShufflerSMI.StartSM();
	}

	

	private void Recharge()
	{
		this.SetConsumed(false);
		this.RequestRecharge(false);
		this.RefreshRechargeChore();
		this.RefreshSideScreen();
	}


    public void RequestRecharge(bool request)
    {
        this.RechargeRequested = request; // 设置充电请求状态
        this.RefreshRechargeChore(); // 刷新充电任务
    }

    private void RefreshRechargeChore()
    {
        this.delivery.Pause(!this.RechargeRequested, "No recharge requested");
    }


    private void SetConsumed(bool consumed)
	{
		this.IsConsumed = consumed;
		this.RefreshConsumedState();
	}

	private void RefreshConsumedState()
	{
		this.geneShufflerSMI.sm.isCharged.Set(!this.IsConsumed, this.geneShufflerSMI, false);
	}




	private void OnStorageChange(object data)
	{
		if (this.storage_recursion_guard)
		{
			return;
		}
		this.storage_recursion_guard = true;
		if (this.IsConsumed)
		{
			for (int i = this.storage.items.Count - 1; i >= 0; i--)
			{
				GameObject gameObject = this.storage.items[i];
                if (!(gameObject == null) && gameObject.IsPrefabID(BuildingBrain.RechargeTag))
				{
                    this.storagegameObject = this.storage.items[i];// 拿到储存大脑对象
                   
                    this.Recharge();
					break;
				}
			}
		}
		this.storage_recursion_guard = false;
	}



	private void DeSelectBuilding()
	{
		if (base.GetComponent<KSelectable>().IsSelected)
		{
			SelectTool.Instance.Select(null, true);
		}
	}

    public void RefreshSideScreen()
    {
        if (base.GetComponent<KSelectable>().IsSelected)
        {
            DetailsScreen.Instance.Refresh(base.gameObject);
        }
    }





    protected override void OnStartWork(WorkerBase worker)
    {
        base.OnStartWork(worker);
        this.notification = new Notification("记忆传承", NotificationType.Good, (List<Notification> notificationList, object data) => "这些复制人受到了传承" + notificationList.ReduceMessages(false), null, false, 0f, null, null, null, true, false, false);
        this.notifier.Add(this.notification, "");
        this.DeSelectBuilding();

    }
    protected override bool OnWorkTick(WorkerBase worker, float dt)
	{
		return base.OnWorkTick(worker, dt);
	}

	protected override void OnAbortWork(WorkerBase worker)
	{
		base.OnAbortWork(worker);
		if (this.chore != null)
		{
			this.chore.Cancel("aborted");
		}
		this.notifier.Remove(this.notification);
	}

	protected override void OnStopWork(WorkerBase worker)
	{
		base.OnStopWork(worker);
		if (this.chore != null)
		{
			this.chore.Cancel("stopped");
		}
		this.notifier.Remove(this.notification);
	}

	protected override void OnCompleteWork(WorkerBase worker)
	{
		base.OnCompleteWork(worker);
	
		this.assignable.Unassign();
		this.DeSelectBuilding();
		this.notifier.Remove(this.notification);
        this.SetConsumed(true);// 工作完成使其变为需要补充

        this.storage.ConsumeIgnoringDisease(this.storagegameObject);// 工作完成移除内部储存的大脑

    }

	



	








	public void SetAssignable(bool set_it)
	{
		this.assignable.SetCanBeAssigned(set_it);//显示或者隐藏选人面板
		this.RefreshSideScreen();
	}

	[MyCmpReq]
	public Assignable assignable;

	[MyCmpAdd]
	public Notifier notifier;

	[MyCmpReq]
	public ManualDeliveryKG delivery;

	[MyCmpReq]
	public Storage storage;

	[Serialize]
	public bool IsConsumed;

	[Serialize]
	public bool RechargeRequested;

	private Chore chore;

    GameObject storagegameObject;


    private BuildingBrainSM.Instance geneShufflerSMI;

	private Notification notification;

	private static Tag RechargeTag = new Tag("KmodMiniBrainCore");

	private static readonly EventSystem.IntraObjectHandler<BuildingBrain> OnStorageChangeDelegate = new EventSystem.IntraObjectHandler<BuildingBrain>(delegate(BuildingBrain component, object data)
	{
		component.OnStorageChange(data);
	});

	private bool storage_recursion_guard;


	public class BuildingBrainSM : GameStateMachine<BuildingBrainSM, BuildingBrainSM.Instance, BuildingBrain>
	{

		public override void InitializeStates(out BaseState default_state)
		{
			default_state = this.consumed;
			this.idle.PlayAnim("on").Enter(delegate(Instance smi)
			{
				smi.master.SetAssignable(true);
			}).Exit(delegate(Instance smi)
			{
				smi.master.SetAssignable(false);
			})
				.WorkableStartTransition((Instance smi) => smi.master, this.working.pre)
				.ParamTransition<bool>(this.isCharged, this.consumed, IsFalse);
			this.working.pre.PlayAnim("working_pre").OnAnimQueueComplete(this.working.loop);
			this.working.loop.PlayAnim("working_loop", KAnim.PlayMode.Loop).ScheduleGoTo(5f, this.working.complete);
			this.working.complete.ToggleStatusItem(Db.Get().BuildingStatusItems.GeneShuffleCompleted, null).Enter(delegate(Instance smi)
			{
				smi.master.RefreshSideScreen();
			}).WorkableStopTransition((Instance smi) => smi.master, this.working.pst);
			this.working.pst.OnAnimQueueComplete(this.consumed);
			this.consumed.PlayAnim("off", KAnim.PlayMode.Once)
				.ParamTransition<bool>(this.isCharged, this.recharging, IsTrue)
                .Enter(delegate (Instance smi)
                 {
                     // 在 consumed 状态中关闭面板
                     smi.master.SetAssignable(false);
                });

			this.recharging.PlayAnim("recharging", KAnim.PlayMode.Once).OnAnimQueueComplete(this.idle);
		}


		public State idle;


		public WorkingStates working;


		public State consumed;

		public	State recharging;

		public BoolParameter isCharged;

		public class WorkingStates : State
		{

			public State pre;

			public State loop;

			public State complete;

			public State pst;
		}

		public new class Instance : GameInstance
        {
			public Instance(BuildingBrain master)
				: base(master)
			{
			}
		}
	}
}
