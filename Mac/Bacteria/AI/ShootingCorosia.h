//
//  ShootingCorosia.h
//  Bacterium
//
//  Created by Giorgi Abelashvili on 3/3/13.
//
//

#import "EnemyAgent.h"
#include <vector>
using namespace std;

@class Bullet;
@interface ShootingCorosia : EnemyAgent
{
    vector <Bullet*> bullets;
}
@end
