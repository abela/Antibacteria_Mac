//
//  Spawner.h
//  Bacterium
//
//  Created by Giorgi Abelashvili on 3/6/13.
//
//

#import "BigBlueCorosia.h"
#include <vector>
//
@class EnemyAgent;
//
@interface Spawner : BigBlueCorosia
{
    vector <EnemyAgent*> spawnuses;
    NSTimer *spawnTimer;
    float spawnTime;
}
@end
