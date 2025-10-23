// Controls/PropertyGridEx.cs
public class PropertyGridEx : PropertyGrid
{
    protected override void OnPropertyValueChanged(PropertyValueChangedEventArgs e)
    {
        base.OnPropertyValueChanged(e);

        // 可以在这里添加自定义验证逻辑
        if (e.ChangedItem.Label == "Id" && (int)e.ChangedItem.Value <= 0)
        {
            MessageBox.Show("ID必须大于0");
            e.ChangedItem.PropertyDescriptor.SetValue(SelectedObject, e.OldValue);
        }
    }
}