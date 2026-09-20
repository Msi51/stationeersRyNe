using System.Xml.Serialization;

public class SerializedReferenceId
{
	[XmlAttribute]
	public long Id;

	public SerializedReferenceId()
	{
	}

	public SerializedReferenceId(long id)
	{
		Id = id;
	}

	public static implicit operator SerializedReferenceId(long id)
	{
		return new SerializedReferenceId(id);
	}

	public static SerializedReferenceId Create(IReferencable sourceReferenceId)
	{
		return new SerializedReferenceId(sourceReferenceId);
	}

	private SerializedReferenceId(IReferencable sourceReferenceId)
	{
		Id = sourceReferenceId?.ReferenceId ?? 0;
	}

	public static implicit operator long(SerializedReferenceId serializedData)
	{
		return serializedData.Id;
	}
}
