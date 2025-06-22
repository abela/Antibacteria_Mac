//
//  LongBacteria.h
//  Bacterium
//
//  Created by Giorgi Abelashvili on 3/8/13.
//
//

#import "EnemyAgent.h"

@interface LongBacteria : EnemyAgent
{
    vector<EnemyAgent*> bodies;
}
-(void) updateBodies;
@end
