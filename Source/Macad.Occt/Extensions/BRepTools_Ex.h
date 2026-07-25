#define Include_BRepTools_History_h \
    BRepTools_History(Macad::Occt::TopoDS_Shape^ shape, BRepBuilderAPI_MakeShape^ algo); \
    BRepTools_History(IEnumerable<Macad::Occt::TopoDS_Shape^>^ shapes, BRepBuilderAPI_MakeShape^ algo); \
    void Merge(Macad::Occt::TopoDS_Shape^ shape, BRepBuilderAPI_MakeShape^ algo); \
    void Merge(IEnumerable<Macad::Occt::TopoDS_Shape^>^ shapes, BRepBuilderAPI_MakeShape^ algo);



