//
//  EnemyDirectionManager.m
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 8/14/13.
//
//

#import "EnemyDirectionManager.h"
#import "EnemyDirection.h"

@implementation EnemyDirectionManager
//
EnemyDirectionManager *_sharedEnemyDirectionManager;
//
+(EnemyDirectionManager*) sharedEnemyDirectionManager
{
    @synchronized([EnemyDirectionManager class])
    {
        if (!_sharedEnemyDirectionManager) {
            _sharedEnemyDirectionManager = [[EnemyDirectionManager alloc] init];
        }
        return _sharedEnemyDirectionManager;
    }
}
//
-(id) init
{
    if ((self = [super init])) {
        return self;
    }
    return nil;
}
//
-(void) createNextEnemyDirectionAtPosition:(CGPoint)position
{
    //
    EnemyDirection *nextEnemyDirection = [[EnemyDirection alloc] addTargetAtPosition:position];
    enemyDirections.push_back(nextEnemyDirection);
    //
}
//
-(void) update
{
    for (int i = 0; i<enemyDirections.size(); i++) {
        if(enemyDirections[i].flag == kDefault)
        {
            [enemyDirections[i] updateTarget];
        }
        else
        {
            [enemyDirections[i] removeTarget];
            [enemyDirections[i] release];
            enemyDirections.erase(enemyDirections.begin() + i);
        }
    }
}
//
-(void) removeEnemyDirection
{
    
}
@end
