//
//  EnemyDirectionManager.h
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 8/14/13.
//
//

#import <Foundation/Foundation.h>
#include <vector>
using namespace std;
//
@class EnemyDirection;
//
@interface EnemyDirectionManager : NSObject
{
    vector<EnemyDirection*> enemyDirections;
}
//
+(EnemyDirectionManager*) sharedEnemyDirectionManager;
-(void) createNextEnemyDirectionAtPosition:(CGPoint)position;
-(void) update;
//
@end
