/// Maded by Gorox for Enterprise. See CLA
using System.Linq;
using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Server.Ganimed.Heretic.Components;
using Content.Shared.Ganimed.Heretic;
using Content.Shared.Ganimed.Heretic.Components;
using Content.Server.Corvax.HiddenDescription;
using Content.Shared.Damage;
using Content.Shared.Tag;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Clothing;
using Content.Shared.Store.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.FixedPoint;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.Ganimed.Heretic;

public sealed class MansusGraspSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly StoreSystem _store = default!;

    public override void Initialize()
    {

        base.Initialize();

        SubscribeLocalEvent<HereticComponent, MansusGraspEvent>(OnAfterInteract);
        SubscribeLocalEvent<HereticComponent, SucrificeDoAfterEvent>(OnSucrifice);
    }

    private void OnAfterInteract(Entity<HereticComponent> ent, ref MansusGraspEvent args)
    {
      var component = ent.Comp;

      if (!EntityManager.HasComponent<HereticSucrificiedComponent>(args.Target) && EntityManager.HasComponent<HereticCanSucrificiedComponent>(args.Target))
      {
           PrepSucrifice(ent, args.Target, ent);
      }
      else if (_tag.HasTag(args.Target, "Table"))
      {
           Spawn("ClothingOuterHereticHood", Transform(args.Target).Coordinates);
           EntityManager.DeleteEntity(args.Target);
      }
      else if (component.Path == "Void" && (_tag.HasTag(args.Target, "Knife")))
      {
           Spawn("VoidEldrichBlade", Transform(args.Target).Coordinates);
           EntityManager.DeleteEntity(args.Target);
      }
      else if (component.Path == "None" && (_tag.HasTag(args.Target, "Knife")))
      {
           component.Path = "Void";
           Spawn("VoidEldrichBlade", Transform(args.Target).Coordinates);
           EntityManager.DeleteEntity(args.Target);

           if (ent != null && TryComp<StoreComponent>(ent, out var storeComp))
           storeComp.Categories.Add("HereticVoidAbilities");
      }
      else if (component.Path == "Ash" && (_tag.HasTag(args.Target, "Matchstick")))
      {
           Spawn("AshEldrichBlade", Transform(args.Target).Coordinates);
           EntityManager.DeleteEntity(args.Target);
      }
      else if (component.Path == "None" && (_tag.HasTag(args.Target, "Matchstick")))
      {
           component.Path = "Ash";
           Spawn("AshEldrichBlade", Transform(args.Target).Coordinates);
           EntityManager.DeleteEntity(args.Target);

           if (ent != null && TryComp<StoreComponent>(ent, out var storeComp))
           storeComp.Categories.Add("HereticAshAbilities");
      }
    }

    //Sucriface

    private void PrepSucrifice(Entity<HereticComponent> ent, EntityUid target, EntityUid user)
    {
      if (target != null && TryComp<DamageableComponent>(target, out var damage))
      {


        if (damage.TotalDamage >= 200)
        {
          var doAfterEventArgs = new DoAfterArgs(EntityManager, user, 5, new SucrificeDoAfterEvent(), ent, target: target)
          {
              BreakOnWeightlessMove = true,
              BreakOnDamage = true,
              NeedHand = true,
              BreakOnHandChange = true
          };

          if (!_doAfterSystem.TryStartDoAfter(doAfterEventArgs))
              return;
        }
      }
    }

    private void OnSucrifice(Entity<HereticComponent> ent, ref SucrificeDoAfterEvent args)
    {
       if (args.Handled || args.Args.Target == null)
           return;

       var target = args.Args.Target.Value;

       if (ent != null && target != null && TryComp<HereticComponent>(ent, out var heretic))
       {
          heretic.Points += 1;

          if (args.User != null && TryComp<StoreComponent>(args.User, out var storeComp))
          {
              _store.TryAddCurrency(new Dictionary<string, FixedPoint2>
                  { {"HereticKnowledge", 1} }, ent);
          }

          EnsureComp<HereticSucrificiedComponent>(target);
          EntityManager.RemoveComponent<HiddenDescriptionComponent>(target);
       }
    }
}
