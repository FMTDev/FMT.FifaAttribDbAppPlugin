using System.Windows;
using System.Windows.Controls;

namespace FifaAttribDbAppPlugin.WPF
{
    public class FieldEditorTemplateSelector : DataTemplateSelector
    {
        public DataTemplate BoolTemplate { get; set; }
        public DataTemplate Int32Template { get; set; }
        public DataTemplate FloatTemplate { get; set; }
        public DataTemplate ArrayTemplate { get; set; }
        public DataTemplate FloatCurveTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is not FIFAAttribDbField field)
                return base.SelectTemplate(item, container);

            return field.FieldType switch
            {
                FifaAttribDbFieldType.Bool => BoolTemplate,
                FifaAttribDbFieldType.Int32 => Int32Template,
                FifaAttribDbFieldType.Float => FloatTemplate,
                FifaAttribDbFieldType.Array => ArrayTemplate,
                FifaAttribDbFieldType.FloatCurve => FloatCurveTemplate,
                FifaAttribDbFieldType.FloatCurve2 => FloatCurveTemplate,
                _ => FloatTemplate
            };
        }
    }
}
