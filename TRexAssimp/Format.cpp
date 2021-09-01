#include "Format.h"

TRexAssimp::Format^ TRexAssimp::Format::FromAIExport(const aiExportFormatDesc* const d)
{
    return gcnew Format(gcnew String(d->id), gcnew String(d->fileExtension), gcnew String(d->description));
}

IEnumerable<TRexAssimp::Format^>^ TRexAssimp::Format::FilterByExtension(IEnumerable<Format^>^ formats, String^ extension)
{
    List<Format^>^ list = gcnew List<Format^>(2);
    auto lowerExtension = extension->ToLower();
    for (auto en_formats = formats->GetEnumerator(); en_formats->MoveNext();)
    {
        Format^ f = en_formats->Current;
        if (f->Extension->ToLower()->Equals(lowerExtension))
            list->Add(f);
    }
    return list;
}

TRexAssimp::Format::Format(String^ id, String^ ext, String^ descp)
{
    if (nullptr == id)
        throw gcnew System::ArgumentNullException("id");

    this->id = id;
    this->extension = ext;
    this->description = descp;
}

bool TRexAssimp::Format::Equals(Object^ o)
{
    auto f = dynamic_cast<Format^>(o);
    if (nullptr != f)
        return Equals(f);
    else
        return false;
}

String^ TRexAssimp::Format::ToString()
{
    return gcnew String("(" + id + ") " + description);
}
