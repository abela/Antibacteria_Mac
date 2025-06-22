//
//  Chorux.h
//  Bacterium
//
//  Created by Giorgi Abelashvili on 3/5/13.
//
//

#import "RedNoise.h"
#include <vector>
using namespace std;
//
@class ChoruxDefender;
//
@interface Chorux : RedNoise
{
    vector<ChoruxDefender*> defenders;
}
@end
