//
//  SpawnManager.h
//  Bacterium
//
//  Created by Giorgi Abelashvili on 2/27/13.
//
//

#import <Foundation/Foundation.h>
#include <vector>
using namespace std;

typedef struct
{
    CGPoint startPoint;
    CGPoint endPoint;
    float rotAlpha;
}
GridiumSpawn;

@interface SpawnManager : NSObject
+(SpawnManager*) sharedSpawnManager;
-(CGPoint) getValidOuterSpawn;
-(CGPoint) getValidSpawnPosFromCharacter;
-(CGPoint) getCircleCoordSpawnWithAlpha:(float) alpha;
-(CGPoint) getRandomPointAroundPoint:(CGPoint)randomPoint;
-(CGPoint) createRandomAgentAtRandomPos;
-(vector<GridiumSpawn>) getGridiumPoints:(int) gridiumsCount gridStartSpawn:(int)startSpawn;
-(CGPoint) getFrunctusValidEvadePosition:(CGPoint)characterPosition;
-(CGPoint) generateValidBonusSpawn;
@end


