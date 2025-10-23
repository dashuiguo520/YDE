using System.ComponentModel;

namespace YamlDataEditor.Models
{
    public class Item
    {
        [Category("基本信息")]
        [Description("物品ID")]
        public int Id { get; set; }

        [Category("基本信息")]
        [Description("Aegis名称")]
        public string AegisName { get; set; } = string.Empty;

        [Category("基本信息")]
        [Description("显示名称")]
        public string Name { get; set; } = string.Empty;

        [Category("基本信息")]
        [Description("物品类型")]
        public string Type { get; set; } = string.Empty;

        [Category("基本信息")]
        [Description("物品子类型")]
        public string SubType { get; set; } = string.Empty;

        [Category("属性")]
        [Description("购买价格")]
        public int? Buy { get; set; }

        [Category("属性")]
        [Description("出售价格")]
        public int? Sell { get; set; }

        [Category("属性")]
        [Description("重量")]
        public int? Weight { get; set; }

        [Category("属性")]
        [Description("攻击力")]
        public int? Attack { get; set; }

        [Category("属性")]
        [Description("魔法攻击力")]
        public int? MagicAttack { get; set; }

        [Category("属性")]
        [Description("攻击范围")]
        public int? Range { get; set; }

        [Category("属性")]
        [Description("插槽数量")]
        public int? Slots { get; set; }

        [Category("职业限制")]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        [Description("职业限制要求")]
        public JobRequirements Jobs { get; set; } = new JobRequirements();

        [Category("装备位置")]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        [Description("装备位置要求")]
        public LocationRequirements Locations { get; set; } = new LocationRequirements();

        [Category("属性")]
        [Description("武器等级")]
        public int? WeaponLevel { get; set; }

        [Category("属性")]
        [Description("最低装备等级")]
        public int? EquipLevelMin { get; set; }

        [Category("属性")]
        [Description("是否可精炼")]
        public bool? Refineable { get; set; }

        [Category("脚本")]
        [Editor(typeof(System.ComponentModel.Design.MultilineStringEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [Description("物品脚本")]
        public string Script { get; set; } = string.Empty;

        [Category("脚本")]
        [Editor(typeof(System.ComponentModel.Design.MultilineStringEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [Description("装备时执行的脚本")]
        public string EquipScript { get; set; } = string.Empty;

        [Category("脚本")]
        [Editor(typeof(System.ComponentModel.Design.MultilineStringEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [Description("卸下时执行的脚本")]
        public string UnEquipScript { get; set; } = string.Empty;

        [Category("交易限制")]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        [Description("交易限制")]
        public TradeRestrictions Trade { get; set; } = new TradeRestrictions();

        // 重写ToString方法用于调试
        public override string ToString()
        {
            return $"Item[Id={Id}, Name={Name}, AegisName={AegisName}]";
        }
    }

    public class JobRequirements
    {
        [Description("炼金术士")]
        public bool Alchemist { get; set; }

        [Description("弓箭手")]
        public bool Archer { get; set; }

        [Description("刺客")]
        public bool Assassin { get; set; }

        [Description("剑士")]
        public bool Swordman { get; set; }

        [Description("法师")]
        public bool Mage { get; set; }

        [Description("商人")]
        public bool Merchant { get; set; }

        [Description("服事")]
        public bool Acolyte { get; set; }

        [Description("盗贼")]
        public bool Thief { get; set; }

        public override string ToString()
        {
            return "职业限制配置";
        }
    }

    public class LocationRequirements
    {
        [Description("右手")]
        public bool Right_Hand { get; set; }

        [Description("双手")]
        public bool Both_Hand { get; set; }

        [Description("头部")]
        public bool Head { get; set; }

        [Description("身体")]
        public bool Body { get; set; }

        [Description("披风")]
        public bool Garment { get; set; }

        [Description("鞋子")]
        public bool Shoes { get; set; }

        [Description("装饰品")]
        public bool Accessory { get; set; }

        public override string ToString()
        {
            return "装备位置配置";
        }
    }

    public class TradeRestrictions
    {
        [Description("不可丢弃")]
        public bool NoDrop { get; set; }

        [Description("不可交易")]
        public bool NoTrade { get; set; }

        [Description("不可出售")]
        public bool NoSell { get; set; }

        [Description("不可存入仓库")]
        public bool NoStorage { get; set; }

        [Description("不可露天商店")]
        public bool NoVend { get; set; }

        [Description("不可交易室")]
        public bool NoTradeRoom { get; set; }

        [Description("不可放入手推车")]
        public bool NoCart { get; set; }

        [Description("不可放入公会仓库")]
        public bool NoGuildStorage { get; set; }

        [Description("不可邮寄")]
        public bool NoMail { get; set; }

        [Description("不可拍卖")]
        public bool NoAuction { get; set; }

        public override string ToString()
        {
            return "交易限制配置";
        }
    }
}