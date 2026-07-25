#include "OcctPCH.h"
#include "..\Generated\TopoDS.h"
#include "..\Generated\BRepTools.h"
#include "..\Generated\BRepBuilderAPI.h"

//--------------------------------------------------------------------------------------------------

Macad::Occt::BRepTools_History::BRepTools_History(Macad::Occt::TopoDS_Shape^ shape, BRepBuilderAPI_MakeShape^ algo)
    : Macad::Occt::Standard_Transient(BaseClass::InitMode::Uninitialized)
{
    ::NCollection_List<::TopoDS_Shape> shapeList;
    shapeList.Append(*shape->NativeInstance);
    NativeInstance = new ::BRepTools_History(shapeList, *algo->NativeInstance);
}

//--------------------------------------------------------------------------------------------------

Macad::Occt::BRepTools_History::BRepTools_History(IEnumerable<Macad::Occt::TopoDS_Shape^>^ shapes, BRepBuilderAPI_MakeShape^ algo)
    : Macad::Occt::Standard_Transient(BaseClass::InitMode::Uninitialized)
{
    ::NCollection_List<::TopoDS_Shape> shapeList;
    for each (auto shape in shapes)
    {
        shapeList.Append(*shape->NativeInstance);
    }
    NativeInstance = new ::BRepTools_History(shapeList, *algo->NativeInstance);
}

//--------------------------------------------------------------------------------------------------

void Macad::Occt::BRepTools_History::Merge(Macad::Occt::TopoDS_Shape^ shape, BRepBuilderAPI_MakeShape^ algo)
{
    ::NCollection_List<::TopoDS_Shape> shapeList;
    shapeList.Append(*shape->NativeInstance);
    ((::BRepTools_History*)_NativeInstance)->Merge(shapeList, *algo->NativeInstance);
}

//--------------------------------------------------------------------------------------------------

void Macad::Occt::BRepTools_History::Merge(IEnumerable<Macad::Occt::TopoDS_Shape^>^ shapes, BRepBuilderAPI_MakeShape^ algo)
{
    ::NCollection_List<::TopoDS_Shape> shapeList;
    for each (auto shape in shapes)
    {
        shapeList.Append(*shape->NativeInstance);
    }
    ((::BRepTools_History*)_NativeInstance)->Merge(shapeList, *algo->NativeInstance);
}
