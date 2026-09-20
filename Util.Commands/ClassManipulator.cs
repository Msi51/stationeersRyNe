using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Assets.Scripts;

namespace Util.Commands;

internal abstract class ClassManipulator<T> : CommandBase
{
	private const BindingFlags FLAGS = BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public;

	public override string[] Arguments => new string[3] { "list", "print", "<PropertyName> <Value>" };

	public override bool IsLaunchCmd => true;

	protected abstract T ObjectInstance { get; }

	protected abstract void OnValueChanged();

	public override string Execute(string[] args)
	{
		switch (args.Length)
		{
		case 0:
			return ListProperties();
		case 1:
		{
			string text = args[0];
			if (text == "list" || text == "l")
			{
				return ListProperties();
			}
			text = args[0];
			if (text == "print" || text == "p")
			{
				return Print();
			}
			return PrintValue(args[0]);
		}
		case 2:
			return SetNewValue(args[0], args[1]);
		default:
		{
			for (int i = 0; i < args.Length; i += 2)
			{
				if (i + 1 == args.Length)
				{
					ConsoleWindow.PrintError(args[i] + " was not assigned a value", suppressStacktrace: true);
				}
				else
				{
					SetNewValue(args[i], args[i + 1]);
				}
			}
			return null;
		}
		}
	}

	private string SetNewValue(string propName, object value)
	{
		MemberInfo memberInfo = typeof(T).GetMember(propName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public).FirstOrDefault();
		if (memberInfo == null)
		{
			return "Property not found: " + propName + ".\nHere is a list of them:\n" + ListProperties();
		}
		object value2;
		if (!(memberInfo is FieldInfo fieldInfo))
		{
			if (!(memberInfo is PropertyInfo propertyInfo))
			{
				throw new ArgumentException();
			}
			value2 = propertyInfo.GetValue(ObjectInstance);
		}
		else
		{
			value2 = fieldInfo.GetValue(ObjectInstance);
		}
		object arg = value2;
		Type type;
		if (!(memberInfo is FieldInfo fieldInfo2))
		{
			if (!(memberInfo is PropertyInfo propertyInfo2))
			{
				throw new ArgumentException();
			}
			type = propertyInfo2.PropertyType;
		}
		else
		{
			type = fieldInfo2.FieldType;
		}
		Type propType = type;
		if (!TryConvertNewValue(value, propType, out var newVal))
		{
			return null;
		}
		ConsoleWindow.PrintAction($"Changed setting '{propName}' from '{arg}' to '{newVal}'");
		if (!(memberInfo is FieldInfo fieldInfo3))
		{
			if (memberInfo is PropertyInfo propertyInfo3)
			{
				propertyInfo3.SetValue(ObjectInstance, newVal);
			}
		}
		else
		{
			fieldInfo3.SetValue(ObjectInstance, newVal);
		}
		OnValueChanged();
		return null;
	}

	private static bool TryConvertNewValue(object value, Type propType, out object newVal)
	{
		try
		{
			newVal = Convert.ChangeType(value, propType);
			return true;
		}
		catch (Exception ex)
		{
			ConsoleWindow.PrintError(ex.Message);
			newVal = null;
			return false;
		}
	}

	private static string ListProperties()
	{
		IEnumerable<string> values = from x in typeof(T).GetMembers()
			select x.Name;
		return string.Join(", ", values);
	}

	private string PrintValue(string propName)
	{
		MemberInfo memberInfo = typeof(T).GetMember(propName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public).FirstOrDefault();
		if (memberInfo == null)
		{
			return "Property not found: " + propName + ".\nHere is a list of them:\n" + ListProperties();
		}
		if (!(memberInfo is FieldInfo fieldInfo))
		{
			if (memberInfo is PropertyInfo propertyInfo)
			{
				return $"{memberInfo.Name}: {propertyInfo.GetValue(ObjectInstance)}";
			}
			throw new ArgumentException();
		}
		return $"{memberInfo.Name}: {fieldInfo.GetValue(ObjectInstance)}";
	}

	private string Print()
	{
		MemberInfo[] members = typeof(T).GetMembers();
		StringBuilder stringBuilder = new StringBuilder();
		MemberInfo[] array = members;
		foreach (MemberInfo memberInfo in array)
		{
			if (!(memberInfo is FieldInfo fieldInfo))
			{
				if (memberInfo is PropertyInfo propertyInfo)
				{
					stringBuilder.AppendLine($"{memberInfo.Name}: {propertyInfo.GetValue(ObjectInstance)}");
				}
			}
			else
			{
				stringBuilder.AppendLine($"{memberInfo.Name}: {fieldInfo.GetValue(ObjectInstance)}");
			}
		}
		return stringBuilder.ToString();
	}
}
