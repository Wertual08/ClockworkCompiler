using ResourceCompiler;
using Resources;
using Resources.Entity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ResourceCompiler.Compilers
{
    class EntityCompiler : Compiler
    {
        private struct CHinge
        {
            public float OffsetX;
            public float OffsetY;
            public int MainNode;
        }
        private void Compile(List<Hinge> hinges)
        {
            Writer.Write(hinges.Count);
            foreach (var hinge in hinges)
            {
                var chinge = new CHinge();
                chinge.OffsetX = hinge.OffsetX;
                chinge.OffsetY = hinge.OffsetY;
                chinge.MainNode = hinge.MainNode;
                Writer.WriteStruct(chinge);
            }
        }

        private struct CFrameNode
        {
            public int Properties;
            public float OffsetX;
            public float OffsetY;
            public float Angle;
        }
        private struct CAnimation
        {
            public int Dependency;
            public float FramesPerUnitRatio;
        }
        private void Compile(Animation src)
        {
            var canimation = new CAnimation();
            canimation.Dependency = (int)src.Dependency;
            canimation.FramesPerUnitRatio = src.FramesPerUnitRatio;
            Writer.WriteStruct(canimation);

            Writer.Write(src.Count);
            foreach (var frame in src.Frames)
            {
                for (int n = 0; n < frame.Count; n++)
                {
                    var node = frame[n];
                    var cnode = new CFrameNode();
                    cnode.Properties = (int)node.Properties;
                    cnode.OffsetX = node.OffsetX;
                    cnode.OffsetY = node.OffsetY;
                    cnode.Angle = node.Angle;
                    Writer.WriteStruct(cnode);
                }
            }
        }

        private struct COutfitNode
        {
            public SpriteCompiler.CSprite Sprite;
            public int RagdollNode;
            public int ClotheType;
        }
        private struct COutfit
        {
            public int ID;
        }
        private void Compile(Outfit outfit, string path)
        {
            var coutfit = new COutfit();
            coutfit.ID = Table["Entity.Outfit", outfit.Name];
            Writer.WriteStruct(coutfit);

            Writer.Write(outfit.Count);
            foreach (var node in outfit.Nodes)
            {
                var cnode = new COutfitNode();
                cnode.Sprite = SpriteCompiler.Compile(Texture, node.Sprite, RootDirectory, path);
                cnode.RagdollNode = node.RagdollNode;
                cnode.ClotheType = (int)node.ClotheType;
                Writer.WriteStruct(cnode);
            }
        }

        private struct CTrigger
        {
            public int ID;

            public double VelocityXLowBound;
            public double VelocityYLowBound;

            public double VelocityXHighBound;
            public double VelocityYHighBound;

            public double AccelerationXLowBound;
            public double AccelerationYLowBound;

            public double AccelerationXHighBound;
            public double AccelerationYHighBound;

            public int OnGround;
            public int OnRoof;
            public int OnLeftWall;
            public int OnRightWall;
            public int FaceLeft;
            public int FaceRight;

            public int AnimationID;
        }
        private void Compile(EntityResource res, Trigger trigger)
        {
            var ctrigger = new CTrigger();

            ctrigger.ID = Table["Entity.Trigger", trigger.Name];

            ctrigger.VelocityXLowBound = trigger.VelocityXLowBound;
            ctrigger.VelocityYLowBound = trigger.VelocityYLowBound;

            ctrigger.VelocityXHighBound = trigger.VelocityXHighBound;
            ctrigger.VelocityYHighBound = trigger.VelocityYHighBound;

            ctrigger.AccelerationXLowBound = trigger.AccelerationXLowBound;
            ctrigger.AccelerationYLowBound = trigger.AccelerationYLowBound;

            ctrigger.AccelerationXHighBound = trigger.AccelerationXHighBound;
            ctrigger.AccelerationYHighBound = trigger.AccelerationYHighBound;

            ctrigger.OnGround = (int)trigger.OnGround;
            ctrigger.OnRoof = (int)trigger.OnRoof;
            ctrigger.OnLeftWall = (int)trigger.OnLeftWall;
            ctrigger.OnRightWall = (int)trigger.OnRightWall;
            ctrigger.FaceLeft = (int)trigger.FaceLeft;
            ctrigger.FaceRight = (int)trigger.FaceRight;

            ctrigger.AnimationID = res.Animations.FindIndex((Animation a) => a.Name == trigger.Animation);

            Writer.WriteStruct(ctrigger);
        }

        private struct CHolder
        {
            public int ID;
            public int Slot;
            public int HolderPoint;
            public int AnimationID;
        }
        private void Compile(EntityResource res, Holder holder)
        {
            var cholder = new CHolder();

            cholder.ID = Table["Entity.Holder", holder.Name];
            cholder.Slot = holder.Slot;
            cholder.HolderPoint = holder.HolderPoint;
            cholder.AnimationID = res.Animations.FindIndex((Animation a) => a.Name == holder.Animation);

            Writer.WriteStruct(cholder);
        }

        private struct CEntity
        {
            public ulong MaxHealth;
            public ulong MaxEnergy;
            public double Mass;

            public double HitboxW;
            public double HitboxH;
            public double GravityAcceleration;
            public double JumpVelocity;
            public double DragX;
            public double DragY;
            public double SqrDragX;
            public double SqrDragY;
            public double MoveForceX;
            public double MoveForceY;
        }
        public override void Placeholder()
        {
            var centity = new CEntity();
            Writer.WriteStruct(centity);
            Writer.Write(0);
            Writer.Write(0);
            Writer.Write(0);
            Writer.Write(0);
            Writer.Write(0);
        }
        public override void Compile(string path)
        {
            var res = new EntityResource(Path.Combine(RootDirectory, path));

            var centity = new CEntity
            {
                HitboxW = res.HitboxW,
                HitboxH = res.HitboxH,
                MaxHealth = res.MaxHealth,
                MaxEnergy = res.MaxEnergy,
                Mass = res.Mass,
                GravityAcceleration = res.GravityAcceleration,
                JumpVelocity = res.JumpVelocity,
                DragX = res.DragX,
                DragY = res.DragY,
                SqrDragX = res.SqrDragX,
                SqrDragY = res.SqrDragY,
                MoveForceX = res.MoveForceX,
                MoveForceY = res.MoveForceY
            };
            Writer.WriteStruct(centity);

            Compile(res.Ragdoll);

            Writer.Write(res.Animations.Count);
            foreach (var animation in res.Animations)
                Compile(animation);

            Writer.Write(res.Outfits.Count);
            foreach (var outfit in res.Outfits)
                Compile(outfit, path);

            Writer.Write(res.Triggers.Count);
            foreach (var trigger in res.Triggers)
                Compile(res, trigger);

            Writer.Write(res.Holders.Count);
            foreach (var holder in res.Holders)
                Compile(res, holder);
        }
    }
}
