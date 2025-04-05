using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;

namespace PupKarma
{
    internal class OverrideOpCheckBox : OpCheckBox
    {
        public OpCheckBox dependingBox;

        bool inverted;

        public OverrideOpCheckBox(Configurable<bool> config, float posX, float posY, OpCheckBox dependingBox, bool inverted = false) : base(config, posX, posY)
        {
            this.dependingBox = dependingBox;
            this.inverted = inverted;
        }

        public override void Update()
        {
            base.Update();
            bool boxBool = dependingBox.GetValueBool() ^ inverted;
            if (boxBool)
            {
                this.SetValueBool(false);
            }
            greyedOut = boxBool;
        }
    }
}
