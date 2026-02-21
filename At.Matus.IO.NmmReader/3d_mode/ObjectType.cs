namespace At.Matus.IO.NmmReader._3d_mode
{
    // Enumeration for the predefined 3D objects which can be measured with the NMM software.
    public enum ObjectType
    {
        None,   // no 3D object at all
        Free,   // 3D point cloud
        Plane,  // plane with rectangular point grid
        Sphere, // hemisphere 
        Line,   // 3-D line with equidistant points
        Point,  // a single point (?)
        Cylinder    // cylinder shell (not implemented yet)
    }
}
