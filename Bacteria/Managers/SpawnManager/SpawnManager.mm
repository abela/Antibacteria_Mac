//
//  SpawnManager.m
//  Bacterium
//
//  Created by Giorgi Abelashvili on 2/27/13.
//
//

#import "SpawnManager.h"
#import "Utils.h"
#import "AiInput.h"
#import "cocos2d.h"
//
#define OUTER_1          -480.0f
#define OUTER_2          1100.0f

//
@implementation SpawnManager
//
SpawnManager *_sharedSpawnManager;
//
+(SpawnManager*) sharedSpawnManager
{
    @synchronized([SpawnManager class])
    {
        if(!_sharedSpawnManager)
            _sharedSpawnManager = [[SpawnManager alloc] init];
        return _sharedSpawnManager;
    }
}
//
-(id) init
{
    if((self = [super init]))
    {
        return self;
    }
    return nil;
}
//
//
-(CGPoint) createRandomAgentAtRandomPos
{
    CGPoint newRandPoint = CGPointZero;
    //
    int randX = [Utils getRandomNumber:[[CoreSettings sharedCoreSettings] upperLeft].x
                                    to:[[CoreSettings sharedCoreSettings] upperRight].x];
    //
    int randY = [Utils getRandomNumber:[[CoreSettings sharedCoreSettings] bottomRight].y
                                    to:[[CoreSettings sharedCoreSettings] upperRight].y];
    //
    newRandPoint = CGPointMake(randX, randY);
    //
    CGPoint characterPos = [[AiInput sharedAiInput] mainCharacterPosition];
    newRandPoint = ccpAdd(newRandPoint, characterPos);
    //
    float newX = newRandPoint.x;
    float newY = newRandPoint.y;
    //
    if(newX > [[CoreSettings sharedCoreSettings] upperRight].x)
        newX -= 2*[Utils getRandomNumber:ENEMY_CIRCLE_SPAWN_RADIUS to:ENEMY_CIRCLE_SPAWN_RADIUS_2];
    if(newX < [[CoreSettings sharedCoreSettings] upperLeft].x)
        newX += 2*[Utils getRandomNumber:ENEMY_CIRCLE_SPAWN_RADIUS to:ENEMY_CIRCLE_SPAWN_RADIUS_2];
    if(newY > [[CoreSettings sharedCoreSettings] upperRight].y)
        newY -= 2*[Utils getRandomNumber:ENEMY_CIRCLE_SPAWN_RADIUS to:ENEMY_CIRCLE_SPAWN_RADIUS_2];
    if(newY < [[CoreSettings sharedCoreSettings] bottomRight].y)
        newY += 2*[Utils getRandomNumber:ENEMY_CIRCLE_SPAWN_RADIUS to:ENEMY_CIRCLE_SPAWN_RADIUS_2];
    //
    return ccp(newX,newY);
}
//
//
-(CGPoint) getValidOuterSpawn
{
    //
    int fieldSideRandomer = arc4random_uniform(4);
    int randY = 0;
    int randX = 0;
    switch (fieldSideRandomer) {
        case 0:
            randX = [[CoreSettings sharedCoreSettings] upperLeft].x;
            randY = [Utils getRandomNumber:[[CoreSettings sharedCoreSettings] bottomLeft].y
                                        to:[[CoreSettings sharedCoreSettings] upperLeft].y];
            break;
        case 1:
            randX = [Utils getRandomNumber:[[CoreSettings sharedCoreSettings] upperLeft].x
                                        to:[[CoreSettings sharedCoreSettings] upperRight].x];
            randY = [[CoreSettings sharedCoreSettings] upperLeft].y;
            break;
        case 2:
            randX = [[CoreSettings sharedCoreSettings] upperRight].x;
            randY = [Utils getRandomNumber:[[CoreSettings sharedCoreSettings] bottomRight].y
                                        to:[[CoreSettings sharedCoreSettings] upperRight].y];
            break;
        case 3:
            randX = [Utils getRandomNumber:[[CoreSettings sharedCoreSettings] bottomLeft].x
                                        to:[[CoreSettings sharedCoreSettings] bottomRight].x];
            
            randY = [[CoreSettings sharedCoreSettings] bottomRight].y;
            break;
        default:
            break;
    }
    //
    return ccp(randX,randY);
}
//
-(vector<GridiumSpawn>) getGridiumPoints:(int) gridiumsCount gridStartSpawn:(int)startSpawn
{
    //
    vector<GridiumSpawn> spawns;
    int gridStartSide = startSpawn;
    float startX = 0.0f;
    float startY = 0.0f;
    float endX = 0.0f;
    float endY = 0.0f;
    float deltaX = 0.0f;
    float deltaY = 0.0f;
    float rotAlpha = 0.0f;
    //
    float distanceToSpawn = 0.0f;
    //
    switch (gridStartSide)
    {
        case 0:         // upper left - upper right
            distanceToSpawn = (UPPER_RIGHT_X - UPPER_LEFT_X);
            startX = UPPER_LEFT_X + (float)(distanceToSpawn / gridiumsCount) - (float)(distanceToSpawn / gridiumsCount)/2.0f;
            startY = UPPER_LEFT_Y;
            endX = startX;
            endY = BOTTOM_LEFT_Y;
            deltaX = (float)(distanceToSpawn / gridiumsCount);
            deltaY = 0.0f;
            rotAlpha = 180.0f;
            break;
        case 1:         // upper right - bottom right
            distanceToSpawn = (UPPER_RIGHT_Y - BOTTOM_RIGHT_Y);
            startX = UPPER_RIGHT_X;
            startY = BOTTOM_RIGHT_Y + (float)(distanceToSpawn / gridiumsCount) - (float)(distanceToSpawn / gridiumsCount)/2.0f;;
            endX = UPPER_LEFT_X;
            endY = startY;
            deltaX = 0.0f;
            deltaY = (float)(distanceToSpawn / gridiumsCount);
            rotAlpha = -90.0f;
            break;
        case 2:         // bottom left - bottom right
            distanceToSpawn = (BOTTOM_RIGHT_X - BOTTOM_LEFT_X);
            startX = BOTTOM_LEFT_X + (float)(distanceToSpawn / gridiumsCount) - (float)(distanceToSpawn / gridiumsCount)/2.0f;
            startY = BOTTOM_LEFT_Y;
            endX = startX;
            endY = UPPER_LEFT_Y;
            deltaX = (float)(distanceToSpawn / gridiumsCount);
            deltaY = 0.0f;
            rotAlpha = 0.0f;
            break;
        case 3:         // bottom left - upper left
            distanceToSpawn = (UPPER_LEFT_Y - BOTTOM_LEFT_Y);
            startX = BOTTOM_LEFT_X;
            startY = BOTTOM_LEFT_Y + (float)(distanceToSpawn / gridiumsCount) - (float)(distanceToSpawn / gridiumsCount)/2.0f;;
            endX = UPPER_RIGHT_X;
            endY = startY;
            deltaX = 0.0f;
            deltaY = (float)(distanceToSpawn / gridiumsCount);
            rotAlpha = 90.0f;
            break;
        default:
            break;
    }
    //
    for (int i = 0; i<gridiumsCount; i++)
    {
        GridiumSpawn gridiumSpawnPoint;
        CGPoint startPos = ccp(startX,startY);
        CGPoint endPos = ccp(endX,endY);
        gridiumSpawnPoint.startPoint = startPos;
        gridiumSpawnPoint.endPoint = endPos;
        gridiumSpawnPoint.rotAlpha = rotAlpha;
        //
        spawns.push_back(gridiumSpawnPoint);
        //
        startX += deltaX;
        startY += deltaY;
        //
        endX += deltaX;
        endY += deltaY;
        //
    }
    //
    return spawns;
    //
}
//
-(CGPoint) getValidSpawnPosFromCharacter
{
    //
    //
    float alpha = arc4random_uniform(360);
    CGPoint characterPos = [[AiInput sharedAiInput] mainCharacterPosition];
    //
    CGPoint newCoord = ccp([Utils getRandomNumber:ENEMY_CIRCLE_SPAWN_RADIUS
                                               to:ENEMY_CIRCLE_SPAWN_RADIUS_2]*cosf(CC_DEGREES_TO_RADIANS(alpha)),
                           [Utils getRandomNumber:ENEMY_CIRCLE_SPAWN_RADIUS
                                               to:ENEMY_CIRCLE_SPAWN_RADIUS_2]*sinf(CC_DEGREES_TO_RADIANS(alpha)));
    //
    newCoord = ccpAdd(newCoord, characterPos);
    //
    float newX = newCoord.x;
    float newY = newCoord.y;
    //
    if(newX > [[CoreSettings sharedCoreSettings] upperRight].x)
        newX -= 2*[Utils getRandomNumber:ENEMY_CIRCLE_SPAWN_RADIUS
                                      to:ENEMY_CIRCLE_SPAWN_RADIUS_2];
    //
    if(newX < [[CoreSettings sharedCoreSettings] upperLeft].x)
        newX += 2*[Utils getRandomNumber:ENEMY_CIRCLE_SPAWN_RADIUS
                                      to:ENEMY_CIRCLE_SPAWN_RADIUS_2];
    //
    if(newY > [[CoreSettings sharedCoreSettings] upperRight].y)
        newY -= 2*[Utils getRandomNumber:ENEMY_CIRCLE_SPAWN_RADIUS
                                      to:ENEMY_CIRCLE_SPAWN_RADIUS_2];
    //
    if(newY < [[CoreSettings sharedCoreSettings] bottomRight].y)
        newY += 2*[Utils getRandomNumber:ENEMY_CIRCLE_SPAWN_RADIUS
                                      to:ENEMY_CIRCLE_SPAWN_RADIUS_2];
    //
    return ccp(newX,newY);
}
//
-(CGPoint) getCircleCoordSpawnWithAlpha:(float) alpha
{
    CGPoint characterPos = [[AiInput sharedAiInput] mainCharacterPosition];
    //
    CGPoint newCoord = ccp([Utils getRandomNumber:ENEMY_CIRCLE_SPAWN_RADIUS
                                               to:ENEMY_CIRCLE_SPAWN_RADIUS_2]*cosf(CC_DEGREES_TO_RADIANS(alpha)),
                           [Utils getRandomNumber:ENEMY_CIRCLE_SPAWN_RADIUS
                                               to:ENEMY_CIRCLE_SPAWN_RADIUS_2]*sinf(CC_DEGREES_TO_RADIANS(alpha)));
    //
    newCoord = ccpAdd(newCoord, characterPos);
    //
    float newX = newCoord.x;
    float newY = newCoord.y;
    //
    if(newX > [[CoreSettings sharedCoreSettings] upperRight].x)
        newX -= 2*[Utils getRandomNumber:ENEMY_CIRCLE_SPAWN_RADIUS
                                      to:ENEMY_CIRCLE_SPAWN_RADIUS_2];
    //
    if(newX < [[CoreSettings sharedCoreSettings] upperLeft].x)
        newX += 2*[Utils getRandomNumber:ENEMY_CIRCLE_SPAWN_RADIUS
                                      to:ENEMY_CIRCLE_SPAWN_RADIUS_2];
    //
    if(newY > [[CoreSettings sharedCoreSettings] upperRight].y)
        newY -= 2*[Utils getRandomNumber:ENEMY_CIRCLE_SPAWN_RADIUS
                                      to:ENEMY_CIRCLE_SPAWN_RADIUS_2];
    //
    if(newY < [[CoreSettings sharedCoreSettings] bottomRight].y)
        newY += 2*[Utils getRandomNumber:ENEMY_CIRCLE_SPAWN_RADIUS
                                      to:ENEMY_CIRCLE_SPAWN_RADIUS_2];
    //
    return ccp(newX,newY);
}
//
-(CGPoint) generateValidBonusSpawn
{
    //
    //
    float alpha = arc4random_uniform(360);
    float newX = 0.0f;
    float newY = 0.0f;
    CGPoint characterPos = [[AiInput sharedAiInput] mainCharacterPosition];
    //
    CGPoint newCoord = ccp([Utils getRandomNumber:BONUS_SPAWN_RADIUS
                                               to:BONUS_SPAWN_RADIUS_2]*cosf(CC_DEGREES_TO_RADIANS(alpha)),
                           [Utils getRandomNumber:BONUS_SPAWN_RADIUS
                                               to:BONUS_SPAWN_RADIUS_2]*sinf(CC_DEGREES_TO_RADIANS(alpha)));
    //
    // recursion, if generated distance is smaller than some CONST number
    if(ccpDistance(characterPos, newCoord) < BONUS_SPAWN_RADIUS / 2.0f)
    {
        newCoord = [self generateValidBonusSpawn];
    }
    else
    {
        //
        newCoord = ccpAdd(newCoord, characterPos);
        //
        newX = newCoord.x;
        newY = newCoord.y;
        //
        if(newX > [[CoreSettings sharedCoreSettings] upperRight].x)
            newX = [[CoreSettings sharedCoreSettings] upperRight].x - BOUNDER_ESCAPE_DELTA;
        if(newX < [[CoreSettings sharedCoreSettings] upperLeft].x)
            newX = [[CoreSettings sharedCoreSettings] upperLeft].x + BOUNDER_ESCAPE_DELTA;
        if(newY > [[CoreSettings sharedCoreSettings] upperRight].y)
            newY = [[CoreSettings sharedCoreSettings] upperRight].y - BOUNDER_ESCAPE_DELTA;
        if(newY < [[CoreSettings sharedCoreSettings] bottomRight].y)
            newY = [[CoreSettings sharedCoreSettings] bottomRight].y + BOUNDER_ESCAPE_DELTA;
        //
        newCoord = ccp(newX,newY);
    }
    return newCoord;
}
//
-(CGPoint) getFrunctusValidEvadePosition:(CGPoint)characterPosition
{
    CGPoint newRandPoint = CGPointZero;
    //
    float alpha = arc4random_uniform(360);
    newRandPoint = ccp([Utils getRandomNumber:FRUNCTUS_RANDOM_AROUND_POINT
                                           to:FRUNCTUS_RANDOM_AROUND_POINT_2]*cosf(CC_DEGREES_TO_RADIANS(alpha)),
                       [Utils getRandomNumber:FRUNCTUS_RANDOM_AROUND_POINT
                                           to:FRUNCTUS_RANDOM_AROUND_POINT_2]*sinf(CC_DEGREES_TO_RADIANS(alpha)));
    //
    newRandPoint = ccpAdd(characterPosition, newRandPoint);
    float newX = newRandPoint.x;
    float newY = newRandPoint.y;
    if(newX > [[CoreSettings sharedCoreSettings] upperRight].x)
        newX = [[CoreSettings sharedCoreSettings] upperRight].x - BOUNDER_ESCAPE_DELTA;
    if(newX < [[CoreSettings sharedCoreSettings] upperLeft].x)
        newX = [[CoreSettings sharedCoreSettings] upperLeft].x + BOUNDER_ESCAPE_DELTA;
    if(newY > [[CoreSettings sharedCoreSettings] upperRight].y)
        newY = [[CoreSettings sharedCoreSettings] upperRight].y - BOUNDER_ESCAPE_DELTA;
    if(newY < [[CoreSettings sharedCoreSettings] bottomRight].y)
        newY = [[CoreSettings sharedCoreSettings] bottomRight].y + BOUNDER_ESCAPE_DELTA;
    //
    return ccp(arc4random()%300,arc4random()%700);
}
//
-(CGPoint) getRandomPointAroundPoint:(CGPoint)randomPoint
{
    CGPoint newRandPoint = CGPointZero;
    //
    float alpha = arc4random_uniform(360);
    newRandPoint = ccp([Utils getRandomNumber:ENEMY_ARROUND_RANDOM_POINT_RADIUS
                                           to:ENEMY_ARROUND_RANDOM_POINT_RADIUS2]*cosf(CC_DEGREES_TO_RADIANS(alpha)),
                           [Utils getRandomNumber:ENEMY_ARROUND_RANDOM_POINT_RADIUS
                                               to:ENEMY_ARROUND_RANDOM_POINT_RADIUS2]*sinf(CC_DEGREES_TO_RADIANS(alpha)));
    //
    newRandPoint = ccpAdd(randomPoint, newRandPoint);
    float newX = newRandPoint.x;
    float newY = newRandPoint.y;
    //
    if(newX > [[CoreSettings sharedCoreSettings] upperRight].x)
        newX = [[CoreSettings sharedCoreSettings] upperRight].x - BOUNDER_ESCAPE_DELTA;
    if(newX < [[CoreSettings sharedCoreSettings] upperLeft].x)
        newX = [[CoreSettings sharedCoreSettings] upperLeft].x + BOUNDER_ESCAPE_DELTA;
    if(newY > [[CoreSettings sharedCoreSettings] upperRight].y)
        newY = [[CoreSettings sharedCoreSettings] upperRight].y - BOUNDER_ESCAPE_DELTA;
    if(newY < [[CoreSettings sharedCoreSettings] bottomRight].y)
        newY = [[CoreSettings sharedCoreSettings] bottomRight].y + BOUNDER_ESCAPE_DELTA;
    //
    return ccp(newX,newY);

}
//
@end
