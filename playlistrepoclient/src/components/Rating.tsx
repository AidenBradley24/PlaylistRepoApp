import React from 'react';
import { BsStar, BsStarFill, BsStarHalf } from "react-icons/bs";

export interface RatingProps {
    rating: number;
}

const Rating: React.FC<RatingProps> = ({ rating }) => {
    const renderStars = (rating: number) => {

        const wholeStarsCount = Math.floor(rating / 2);
        const hasHalfStar = rating % 2 === 1;
        const emptyStarsCount = 5 - (wholeStarsCount + (hasHalfStar ? 1 : 0));

        const stars = [];

        let key = 0;
        for (let i = 0; i < wholeStarsCount; i++) {
            stars.push(<BsStarFill key={key++} />);
        }

        if (hasHalfStar) {
            stars.push(<BsStarHalf key={key++} />);
        }

        for (let i = 0; i < emptyStarsCount; i++) {
            stars.push(<BsStar key={key++} />);
        }

        return stars;
    };

    return (
        <div className="rating">
            {renderStars(rating)}
        </div>
    );
};

export default Rating;