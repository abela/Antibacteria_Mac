//
//  ZamriGnida.h
//  Bacterium
//
//  Created by Giorgi Abelashvili on 3/10/13.
//
//

#import "Gnida.h"

@interface ZamriGnida : Gnida
{
    float spiralAlpha;
    int radius;
    int alphaDelta;
}
-(void) moveOnCircle;
@end
