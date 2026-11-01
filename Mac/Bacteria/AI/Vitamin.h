//
//  Vitamin.h
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 6/20/13.
//
//

#import "EnemyAgent.h"

@interface Vitamin : EnemyAgent
{
    int vitaminScore;
}
@property (readonly) int vitaminScore;
-(id) createAtPosition:(CGPoint)position withScore:(int)score;
@end
